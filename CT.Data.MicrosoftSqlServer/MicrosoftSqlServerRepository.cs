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

using CT.Data.MicrosoftSqlServer.Properties;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace CT.Data.MicrosoftSqlServer
{
    public class MicrosoftSqlServerRepository : Repository, IMicrosoftSqlServerRepository
    {
        protected MicrosoftSqlServerRepository() {  }

        protected override T OnExecute<T>(DbConnection connection, DbTransaction transaction, string statement)
        {
            var command = new SqlCommand(statement, (SqlConnection)connection)
            {
                Transaction = (SqlTransaction)transaction
            };

            var returnValue = (T)command.ExecuteScalar();
            return returnValue;
        }

        protected override IEnumerable<T> OnLoad<T>(DbConnection connection, DbTransaction transaction, string query)
        {
            var cmd = new SqlCommand(query, (SqlConnection)connection)
            {
                Transaction = (SqlTransaction)transaction
            };

            var dataReader = cmd.ExecuteReader();
            while (dataReader.Read())
            {
                yield return dataReader.ToModel<T>();
            }

            dataReader.Close();
        }

        protected override DbConnection OnNewConnection(string connectionString)
        {
            var newConnection = new SqlConnection(connectionString);
            newConnection.Open();
            return newConnection;
        }

        protected override DbTransaction OnNewTransaction(DbConnection connection)
        {
            return connection.BeginTransaction();
        }

        protected override void OnCommit(DbConnection connection, DbTransaction transaction)
        {
            transaction.Commit();
        }

        protected override void OnDelete(DbConnection connection, DbTransaction transaction, string tableName, IEnumerable<object> idValues)
        {
            foreach (var id in idValues)
            {
                // todo
            }
        }

        protected override void OnSaveUpdate(DbConnection connection, DbTransaction transaction, Composite composite)
        {
            throw new NotImplementedException();
        }

        protected override void OnSaveNew(DbConnection connection, DbTransaction transaction, IReadOnlyList<DataTable> dataTablesToInsert)
        {
            foreach (var dataTable in dataTablesToInsert)
            {
                if (!Regex.IsMatch(dataTable.TableName, @"^[A-Za-z0-9_]+$"))
                    throw new ArgumentException(Resources.InvalidTableName);

                OnExecute<object>(connection, transaction,
                $@"
                    SELECT * INTO #{dataTable.TableName} FROM {dataTable.TableName} WHERE 1 = 0
                    SET IDENTITY_INSERT #{dataTable.TableName} ON
                ");

                using (var sqlBulkCopy = new SqlBulkCopy((SqlConnection)connection))
                {
                    sqlBulkCopy.DestinationTableName = "#" + dataTable.TableName;
                    sqlBulkCopy.WriteToServer(dataTable);
                }

                var mergeSql = $@"
                 
                    MERGE INTO {dataTable.TableName}
                    USING #{dataTable.TableName} AS tableToInsert ON 1 = 0 
                    WHEN NOT MATCHED BY TARGET
                    THEN INSERT({dataTable.ExtendedProperties[nameof(SaveParameters.SqlColumnList)]})
                      VALUES({dataTable.ExtendedProperties[nameof(SaveParameters.SqlInsertColumnList)]})
                    OUTPUT INSERTED.{dataTable.ExtendedProperties[nameof(SaveParameters.ModelKeyPropertyName)]} AS {nameof(InsertKeyPair.InsertedKey)},
                      tableToInsert.{dataTable.ExtendedProperties[nameof(SaveParameters.ModelKeyPropertyName)]} AS {nameof(InsertKeyPair.OriginalKey)};
                ";

                var insertKeyPairs = OnLoad<InsertKeyPair>(connection, transaction, mergeSql);

                OnExecute<object>(connection, transaction, $@"DROP TABLE #{dataTable.TableName}");

                foreach(var insertKeyPair in insertKeyPairs)
                {
                    var row = dataTable.Rows.Find(insertKeyPair.OriginalKey);
                    var model = row["__model"];
                    model.GetType().GetProperty(dataTable.ExtendedProperties[nameof(SaveParameters.ModelKeyPropertyName)] as string).SetValue(model, insertKeyPair.InsertedKey);
                }
            }
        }
    }

    internal class InsertKeyPair
    {
        
        public object InsertedKey { get; set; }
        public object OriginalKey { get; set; }
    }
}
