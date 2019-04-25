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

using CT.Properties;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace CT.Data
{
    public abstract class Repository : IRepository
    {
        public CompositeRoot CompositeRoot { get; set; }

        public DbConnection OpenConnection(string connectionString)
        {
            return OnOpenNewConnection(connectionString);
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

        public T Execute<T>(DbConnection connection, DbTransaction transaction, string statement, IEnumerable<DbParameter> parameters)
        {
            return OnExecute<T>(connection, transaction, statement, parameters);
        }

        public IEnumerable<object> Load(DbConnection connection, DbTransaction transaction, string query, IEnumerable<DbParameter> parameters, Type modelType)
        {
            return OnLoad(connection, transaction, query, parameters, modelType);
        }

        public void Save(DbConnection connection, DbTransaction transaction, Composite composite)
        {
            var newComposites = new List<Composite>();

            composite.TraverseDepthFirst((c) =>
            {
                var compositeType = c.GetType();

                CompositeContainerAttribute compositeDictionaryPropertyAttribute;

                var compositeModelAttribute = compositeType.FindCustomAttribute<CompositeModelAttribute>();
                var modelFieldInfo = compositeType.GetField(compositeModelAttribute?.ModelFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                var dataContractAttribute = modelFieldInfo?.FieldType.GetCustomAttribute<DataContractAttribute>();
                var modelKeyPropertyAttribute = modelFieldInfo?.FieldType.GetCustomAttribute<KeyPropertyAttribute>();
                var modelKeyProperty = modelFieldInfo?.FieldType.GetProperty(modelKeyPropertyAttribute.PropertyName);
                var modelKeyDataMemberAttribute = modelKeyProperty?.GetCustomAttribute<DataMemberAttribute>();

                if ((compositeDictionaryPropertyAttribute = compositeType.FindCustomAttribute<CompositeContainerAttribute>()) != null)
                {
                    var removedIdsProperty = compositeType
                        .GetProperty(compositeDictionaryPropertyAttribute.CompositeContainerDictionaryPropertyName)
                        .GetValue(c)
                        .GetType().GetProperty(nameof(CompositeDictionary<object, Composite>.RemovedIds));

                    var compositeDictionary = compositeType
                        .GetProperty(compositeDictionaryPropertyAttribute.CompositeContainerDictionaryPropertyName)
                        .GetValue(c);

                    if (compositeModelAttribute == null)
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.MustHaveCompositeModelAttribute, compositeType.Name));

                    if (modelFieldInfo == null)
                        throw new MemberAccessException(Resources.CannotFindCompositeModelProperty);

                    if (dataContractAttribute == null)
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.MustHaveDataContractAttribute, modelFieldInfo.FieldType.Name));

                    if (modelKeyPropertyAttribute == null)
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.MustHaveKeyPropertyAttribute, modelFieldInfo.FieldType.Name));

                    if (modelKeyProperty == null)
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidPropertyName, modelKeyPropertyAttribute.PropertyName));

                    if (modelKeyDataMemberAttribute == null)
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.MustHaveDataMemberAttribute, modelKeyPropertyAttribute.PropertyName));

                    var deletedIds = (IEnumerable<object>)removedIdsProperty.GetValue(compositeDictionary);

                    var tableName = dataContractAttribute.Name ?? modelFieldInfo.FieldType.Name;
                    var tableKeyPropertyName = modelKeyDataMemberAttribute.Name ?? modelKeyProperty.Name;

                    if (!Regex.IsMatch(tableName, @"^[A-Za-z0-9_]+$"))
                        throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidTableName, tableName));

                    if (!Regex.IsMatch(tableKeyPropertyName, @"^[A-Za-z0-9_]+$"))
                        throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidColumnName, tableKeyPropertyName));

                    OnDelete(connection, transaction, tableName, tableKeyPropertyName, deletedIds);
                }

                switch (c.State)
                {
                    case CompositeState.Modified:

                        if (modelFieldInfo == null)
                            throw new MemberAccessException(Resources.CannotFindCompositeModelProperty);

                        if (dataContractAttribute == null)
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.MustHaveDataContractAttribute, modelFieldInfo.FieldType.Name));

                        if (modelKeyDataMemberAttribute == null)
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.MustHaveDataMemberAttribute, modelKeyPropertyAttribute.PropertyName));

                        if (modelKeyProperty == null)
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidPropertyName, modelKeyPropertyAttribute.PropertyName));

                        var dataRow = new Composite[] { composite }.ToDataTable().Rows[0];
                        var columnValues = dataRow.Table.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName, column => dataRow[column]);

                        var keyColumnName = modelKeyDataMemberAttribute.Name ?? modelKeyProperty.Name;
                        var keyValue = dataRow[keyColumnName];

                        var tableName = dataContractAttribute.Name ?? modelFieldInfo.FieldType.Name;
                        var tableKeyPropertyName = modelKeyDataMemberAttribute.Name ?? modelKeyProperty.Name;

                        if (!Regex.IsMatch(tableName, @"^[A-Za-z0-9_]+$"))
                            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidTableName, tableName));

                        if (!Regex.IsMatch(tableKeyPropertyName, @"^[A-Za-z0-9_]+$"))
                            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidColumnName, tableKeyPropertyName));

                        string invalidColumnName = null;
                        if ((invalidColumnName = columnValues.Keys.FirstOrDefault(column => !Regex.IsMatch(column, @"^[A-Za-z0-9_]+$"))) != null)
                            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidColumnName, invalidColumnName));

                        OnUpdate(connection, transaction, tableName, tableKeyPropertyName, keyValue, columnValues);
                        composite.State = CompositeState.Unchanged;

                        break;
                    case CompositeState.New:
                        newComposites.Add(c);
                        break;
                    default:
                        break;
                }
            });

            SaveNewComposites(connection, transaction, newComposites);
        }

        private void SaveNewComposites(DbConnection connection, DbTransaction transaction, List<Composite> newComposites)
        {
            var dataTablesToInsert = new List<DataTable>();
            var newCompositeTypes = newComposites.Select(nc => nc.GetType()).Distinct();

            var sqlColumnList = string.Empty;
            var sqlInsertColumnList = string.Empty;

            foreach (var compositeType in newCompositeTypes)
            {
                var compositeModelAttribute = compositeType.FindCustomAttribute<CompositeModelAttribute>();
                if (compositeModelAttribute == null)
                    throw new MissingMemberException(string.Format(CultureInfo.CurrentCulture, Resources.MustHaveCompositeModelAttribute, compositeType.Name));

                FieldInfo modelFieldInfo;
                modelFieldInfo = compositeType.GetField(compositeModelAttribute.ModelFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (modelFieldInfo == null)
                    throw new MissingMemberException(string.Format(CultureInfo.CurrentCulture, Resources.CannotFindCompositeModelProperty, compositeModelAttribute.ModelFieldName));

                var columnProperties = modelFieldInfo
                                        .FieldType
                                        .GetProperties()
                                        .Where(p => p.GetCustomAttribute<DataMemberAttribute>() != null);

                var columnList = columnProperties.Select(dataMemberProperty => dataMemberProperty.GetCustomAttribute<DataMemberAttribute>().Name ?? dataMemberProperty.Name);

                var invalidColumnName = string.Empty;
                if ((invalidColumnName = columnList.FirstOrDefault(c => !Regex.IsMatch(c, @"^[A-Za-z0-9_]+$"))) != null)
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidColumnName, invalidColumnName));

                sqlColumnList = string.Join(',', columnList);
                sqlInsertColumnList = string.Join(',', columnList.Select(c => "tableToInsert." + c));

                var dataTable = newComposites.Where(nc => nc.GetType() == compositeType).ToDataTable();

                var modelKeyPropertyName = modelFieldInfo.FieldType.GetCustomAttribute<KeyPropertyAttribute>()?.PropertyName;
                if (string.IsNullOrEmpty(modelKeyPropertyName))
                    throw new InvalidOperationException();

                var modelKeyName = modelFieldInfo.FieldType.GetProperty(modelKeyPropertyName)?.GetCustomAttribute<DataMemberAttribute>()?.Name ?? modelKeyPropertyName;

                dataTable.ExtendedProperties[nameof(SaveParameters.ModelKeyPropertyName)] = modelKeyName;
                dataTable.ExtendedProperties[nameof(SaveParameters.SqlColumnList)] = sqlColumnList;
                dataTable.ExtendedProperties[nameof(SaveParameters.SqlInsertColumnList)] = sqlInsertColumnList;

                dataTablesToInsert.Add(dataTable);
            }

            string invalidTableName = null;
            if ((invalidTableName = dataTablesToInsert.FirstOrDefault(t => !Regex.IsMatch(t.TableName, @"^[A-Za-z0-9_]+$"))?.TableName) != null)
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidTableName, invalidTableName));

            OnInsert(connection, transaction, dataTablesToInsert);

            foreach (var composite in newComposites)
                composite.State = CompositeState.Unchanged;
        }

        protected abstract DbConnection OnOpenNewConnection(string connectionString);
        protected abstract DbTransaction OnBeginNewTransaction(DbConnection connection);
        protected abstract void OnDelete(DbConnection connection, DbTransaction transaction, string tableName, string tableKeyPropertyName, IEnumerable<object> idValues);
        protected abstract void OnInsert(DbConnection connection, DbTransaction transaction, IReadOnlyList<DataTable> dataTablesToInsert);
        protected abstract void OnUpdate(DbConnection connection, DbTransaction transaction, string tableName, string tableKeyPropertyName, object tableKeyValue, IReadOnlyDictionary<string, object> columnValues);
        protected abstract void OnCommit(DbConnection connection, DbTransaction transaction);
        protected abstract IEnumerable<object> OnLoad(DbConnection connection, DbTransaction transaction, string query, IEnumerable<DbParameter> parameters, Type modelType);
        protected abstract T OnExecute<T>(DbConnection connection, DbTransaction transaction, string statement, IEnumerable<DbParameter> parameters);
    }
}
