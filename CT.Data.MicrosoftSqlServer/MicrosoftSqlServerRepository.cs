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
using System.Data.Common;

namespace CT.Data.MicrosoftSqlServer
{
    public class MicrosoftSqlServerRepository : Repository, IMicrosoftSqlServerRepository
    {
        protected MicrosoftSqlServerRepository() {  }

        protected override void OnCommit(DbConnection connection, DbTransaction transaction)
        {
            throw new NotImplementedException();
        }

        protected override void OnDelete(DbConnection connection, DbTransaction transaction, string tableName, IEnumerable<object> idValues)
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

        protected override DbConnection OnNewConnection()
        {
            throw new NotImplementedException();
        }

        protected override DbTransaction OnNewTransaction(DbConnection connection)
        {
            throw new NotImplementedException();
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
