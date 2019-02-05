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
        }

        protected override void OnDelete(DbConnection connection, DbTransaction transaction, string tableName, IEnumerable<object> idValues)
        {
            //1,2,3,5,8,12,14,15,16,17,18,19,32,56,57,95

            //1,2,3               x >= 1 && x <= 3
            //5                   x == 5
            //8                   x == 8
            //12                  x == 12
            //14,15,16,17,18,19   x >= 14 && x <= 19
            //32                  x == 32
            //56,57               x >= 56 && x <= 57
            //95                  x == 95


            foreach (var id in idValues)
            {
                // todo
            }
        }

        protected override T OnExecute<T>(DbConnection connection, DbTransaction transaction, string statement)
        {
            var command = new SqlCommand(statement, (SqlConnection)connection)
            {
                Transaction = (SqlTransaction)transaction
            };

            var returnValue = (T)command.ExecuteScalar();
            return returnValue;
        }

        protected override IEnumerable<T> OnLoad<T>(DbConnection connection, string query)
        {
            var cmd = new SqlCommand(query, (SqlConnection)connection);

            IDataReader dataReader = cmd.ExecuteReader();
            while (dataReader.Read())
            {
                yield return dataReader.ToModel<T>();
            }

            dataReader.Close();
        }

        protected override DbConnection OnNewConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
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
            //--create table
            //IF NOT EXISTS(SELECT * FROM sys.tables WHERE NAME = 'Test2')
            //    CREATE TABLE Test2(ID INT IDENTITY(1, 1) NOT NULL)

            //-- add column
            //IF NOT EXISTS(SELECT * FROM sys.columns WHERE NAME = 'ColumnName' AND object_id = OBJECT_ID(N'[dbo].[Test2]'))
            //    ALTER TABLE Test2 ADD ColumnName NVARCHAR(MAX)

            //--modify column
            //IF EXISTS(SELECT * FROM sys.columns WHERE NAME = 'ColumnName' AND object_id = OBJECT_ID(N'[dbo].[Test2]'))
            //    ALTER TABLE Test2 ALTER COLUMN ColumnName NVARCHAR(2)


            //--The existing table
            // CREATE TABLE Test (ID INT IDENTITY(1, 1), [Name] NVARCHAR(MAX));
            //
            // -----------------------------------------------------------------
            //            
            // CREATE TABLE #Test (ID INT, [Name] NVARCHAR(MAX))

            // do bulk copy into #Test

            // MERGE INTO Test
            // USING #Test AS tableToInsert ON 1 = 0 
            // WHEN NOT MATCHED BY TARGET
            // THEN INSERT([Name])
            //                VALUES(tableToInsert.[NAME])
            // OUTPUT inserted.ID, tableToInsert.ID;

            // DROP TABLE #Test

            var newRecords = newComposites.ToDataTable();

        }


    }
}
