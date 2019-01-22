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

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace CT.Data.MicrosoftSqlServer
{
    public class MicrosoftSqlServerRepository : Repository, IMicrosoftSqlServerRepository
    {
        protected MicrosoftSqlServerRepository() {  }

        protected override void OnCommit(DbConnection connection, DbTransaction transaction)
        {
            transaction.Commit();
            connection.Close();
        }

        protected override void OnDelete(DbConnection connection, DbTransaction transaction, string tableName, IEnumerable<object> idValues)
        {
            using (var newConnection = OnNewConnection())
            {
                newConnection.Open();
                var newTransaction = OnNewTransaction(newConnection);

                foreach (var id in idValues)
                {
                    // todo
                }

                OnCommit(newConnection, newTransaction);
            }

        }

        protected override T OnExecute<T>(string statement)
        {
            using (var newConnection = OnNewConnection())
            {
                newConnection.Open();
                var newTransaction = OnNewTransaction(newConnection);
                var command = new SqlCommand(statement, (SqlConnection)newConnection)
                {
                    Transaction = (SqlTransaction)newTransaction
                };
                var returnValue = (T)command.ExecuteScalar();
                OnCommit(newConnection, newTransaction);
                return returnValue;
            }
        }

        protected override IEnumerable<T> OnLoad<T>(string query)
        {
            using (var newConnection = OnNewConnection())
            {
                var cmd = new SqlCommand(query, (SqlConnection)newConnection);
                newConnection.Open();

                IDataReader dataReader = cmd.ExecuteReader();
                while (dataReader.Read())
                {
                    yield return dataReader.ToModel<T>();
                }

                dataReader.Close();
            }
        }

        protected override DbConnection OnNewConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        protected override DbTransaction OnNewTransaction(DbConnection connection)
        {
            return connection.BeginTransaction();
        }

        protected override void OnSaveUpdate(DbConnection connection, DbTransaction transaction, Composite composite)
        {
            throw new NotImplementedException();
        }

        protected override void OnSaveNew(DbConnection connection, DbTransaction transaction, IEnumerable<Composite> newComposites)
        {
            throw new NotImplementedException();
        }


    }
}
