// Compositactic - Made in the USA - Indianapolis, IN  - Copyright (c) 2017 Matt J. Crouch

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction, 
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
// subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies
// or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN 
// NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Data.Common;

namespace CT.Data
{
    public abstract class Repository : IRepository
    {
        public CompositeRoot CompositeRoot { get; set; }

        public DbConnection OpenConnection(string connectionString)
        {
            return OnNewConnection(connectionString);
        }

        public DbTransaction BeginTransaction(DbConnection connection)
        {
            return connection.BeginTransaction();
        }

        public void CommitTransaction(DbTransaction transaction)
        {
            transaction.Commit();
        }

        public void CloseConnection(DbConnection connection)
        {
            connection.Close();
        }

        public T Execute<T>(DbConnection connection, DbTransaction transaction, string statement)
        {
            return OnExecute<T>(connection, transaction, statement);
        }

        public IEnumerable<T> Load<T>(DbConnection connection, string query) where T : new()
        {
            return OnLoad<T>(connection, query);
        }

        public void Save(DbConnection connection, DbTransaction transaction, Composite composite)
        {
            var newComposites = new List<Composite>();
            IEnumerable<object> deletedIds = null;

            composite.TraverseBreadthFirst((c) =>
            {
                var compositeType = c.GetType();

                CompositeDictionaryPropertyAttribute compositeDictionaryPropertyAttribute;
                if ((compositeDictionaryPropertyAttribute = compositeType.FindCustomAttribute<CompositeDictionaryPropertyAttribute>()) != null)
                {
                    deletedIds = (IEnumerable<object>)compositeType
                        .GetProperty(compositeDictionaryPropertyAttribute.CompositeDictionaryPropertyName)
                        .GetValue(c)
                        .GetType().GetProperty("RemovedIds").GetValue(c);
                }
                else
                    deletedIds = new object[] { };

                switch (composite.State)
                {
                    case CompositeState.Modified:
                        OnSaveUpdate(connection, transaction, composite);
                        break;
                    case CompositeState.New:
                        newComposites.Add(c);
                        break;
                    default:
                        break;
                }
            });

            OnDelete(connection, transaction, composite.GetType().Name, deletedIds);
            OnSaveNew(connection, transaction, newComposites);
        }

        protected abstract DbConnection OnNewConnection(string connectionString);
        protected abstract DbTransaction OnNewTransaction(DbConnection connection);
        protected abstract void OnDelete(DbConnection connection, DbTransaction transaction, string tableName, IEnumerable<object> idValues);
        protected abstract void OnSaveNew(DbConnection connection, DbTransaction transaction, IEnumerable<Composite> newComposites);
        protected abstract void OnSaveUpdate(DbConnection connection, DbTransaction transaction, Composite composite);
        protected abstract void OnCommit(DbConnection connection, DbTransaction transaction);
        protected abstract IEnumerable<T> OnLoad<T>(DbConnection connection, string query) where T : new();
        protected abstract T OnExecute<T>(DbConnection connection, DbTransaction transaction, string statement);
    }
}
