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
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CT.Data.MicrosoftSqlServer
{
    public class MicrosoftSqlServerRepository : Repository, IMicrosoftSqlServerRepository
    {
        protected MicrosoftSqlServerRepository() {  }

        protected override T OnExecute<T>(DbConnection connection, DbTransaction transaction, string statement, IEnumerable<DbParameter> parameters)
        {
            var command = new SqlCommand(statement, (SqlConnection)connection)
            {
                Transaction = (SqlTransaction)transaction
            };

            if (parameters != null)
                command.Parameters.AddRange(parameters.ToArray());

            var returnValue = (T)command.ExecuteScalar();
            return returnValue;
        }

        protected override IEnumerable<T> OnLoad<T>(DbConnection connection, DbTransaction transaction, string query, IEnumerable<DbParameter> parameters)
        {
            var command = new SqlCommand(query, (SqlConnection)connection)
            {
                Transaction = (SqlTransaction)transaction
            };

            if (parameters != null)
                command.Parameters.AddRange(parameters.ToArray());

            var dataReader = command.ExecuteReader();
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

        protected override void OnDelete(DbConnection connection, DbTransaction transaction, string tableName, string tableKeyPropertyName, IEnumerable<object> idValues)
        {
            if (!Regex.IsMatch(tableName, @"^[A-Za-z0-9_]+$"))
                throw new ArgumentException(Resources.InvalidTableName);

            int batchSize = 500;

            var batches = idValues
                    .Select((item, inx) => new { item, inx })
                    .GroupBy(x => x.inx / batchSize)
                    .Select(g => g.Select(x => x.item));

            var sqlStatement = $@"DELETE FROM {tableName} WHERE {tableKeyPropertyName} IN ";

            var parameterIndex = 0;
            foreach (var batch in batches)
            {
                var parameterList = "(" + string.Join(',', batch.Select(id => "@p" + parameterIndex++)) + ")";
                parameterIndex = 0;
                var parameters = batch.Select(id => new SqlParameter("@p" + parameterIndex++, id.ToString()));
                sqlStatement += parameterList;
                OnExecute<object>(connection, transaction, sqlStatement, parameters);
            }
        }

        protected override void OnUpdate(DbConnection connection, DbTransaction transaction, string tableName, string tableKeyPropertyName, IReadOnlyDictionary<string, object> modifiedColumns)
        {
            throw new NotImplementedException();
        }

        protected override void OnInsert(DbConnection connection, DbTransaction transaction, IReadOnlyList<DataTable> dataTablesToInsert)
        {
            foreach (var dataTable in dataTablesToInsert)
            {
                if (!Regex.IsMatch(dataTable.TableName, @"^[A-Za-z0-9_]+$"))
                    throw new ArgumentException(Resources.InvalidTableName);

                OnExecute<object>(connection, transaction,
                $@"

                    SELECT * INTO #{dataTable.TableName} FROM {dataTable.TableName} WHERE 1 = 0
                    SET IDENTITY_INSERT #{dataTable.TableName} ON

                ", null);

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

                var insertKeyPairs = OnLoad<InsertKeyPair>(connection, transaction, mergeSql, null);

                OnExecute<object>(connection, transaction, $@"DROP TABLE #{dataTable.TableName}", null);

                var modelKeyPropertyName = dataTable.ExtendedProperties[nameof(SaveParameters.ModelKeyPropertyName)] as string;
                PropertyInfo modelKeyProperty = null;

                foreach (var insertKeyPair in insertKeyPairs)
                {
                    var row = dataTable.Rows.Find(insertKeyPair.OriginalKey);
                    var model = row["__model"];

                    if (modelKeyProperty == null)
                        modelKeyProperty = model.GetType().GetProperty(modelKeyPropertyName);

                    modelKeyProperty.SetValue(model, insertKeyPair.InsertedKey);
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
