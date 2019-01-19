using System;
using System.Collections.Generic;
using System.Data.Common;

namespace CT.Data.MicrosoftSqlServer
{
    public class MicrosoftSqlServerRepository : Repository
    {
        private MicrosoftSqlServerRepository() {  }

        protected override void OnCommit(DbConnection connection, DbTransaction transaction)
        {
            throw new NotImplementedException();
        }

        protected override void OnDelete(DbConnection connection, DbTransaction transaction, IEnumerable<object> idValues)
        {
            throw new NotImplementedException();
        }

        protected override T OnExecute<T>(string statement)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<Composite> OnLoad(string query)
        {
            throw new NotImplementedException();
        }

        protected override void OnSave(DbConnection connection, DbTransaction transaction, Composite composite)
        {
            throw new NotImplementedException();
        }

        protected override void OnSaveNew(DbConnection connection, DbTransaction transaction, IEnumerable<Composite> newComposites)
        {
            throw new NotImplementedException();
        }
    }
}
