using System;
using System.Collections.Generic;
using System.Data.Common;

namespace CT.Data
{
    public abstract class Repository : IRepository
    {
        public string ConnectionString { get; set; }

        public CompositeRoot CompositeRoot { get; set; }

        public T Execute<T>(string statement)
        {
            return OnExecute<T>(statement);
        }

        public IEnumerable<Composite> Load(string query)
        {
            return OnLoad(query);
        }

        public void Save(Composite composite)
        {
            throw new NotImplementedException();
        }

        public void UpdateSchema<T>() where T : Composite
        {
            throw new NotImplementedException();
        }

        protected abstract void OnDelete(DbConnection connection, DbTransaction transaction, IEnumerable<object> idValues);
        protected abstract void OnSaveNew(DbConnection connection, DbTransaction transaction, IEnumerable<Composite> newComposites);
        protected abstract void OnSave(DbConnection connection, DbTransaction transaction, Composite composite);
        protected abstract void OnCommit(DbConnection connection, DbTransaction transaction);
        protected abstract IEnumerable<Composite> OnLoad(string query);
        protected abstract T OnExecute<T>(string statement);
    }
}
