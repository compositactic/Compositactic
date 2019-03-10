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

        public T Execute<T>(DbConnection connection, DbTransaction transaction, string statement, IEnumerable<DbParameter> parameters)
        {
            return OnExecute<T>(connection, transaction, statement, parameters);
        }

        public IEnumerable<T> Load<T>(DbConnection connection, DbTransaction transaction, string query, IEnumerable<DbParameter> parameters) where T : new()
        {
            return OnLoad<T>(connection, transaction, query, parameters);
        }

        public void Save(DbConnection connection, DbTransaction transaction, Composite composite)
        {
            var newComposites = new List<Composite>();

            composite.TraverseBreadthFirst((c) =>
            {
                var compositeType = c.GetType();

                CompositeDictionaryPropertyAttribute compositeDictionaryPropertyAttribute;
                CompositeModelAttribute compositeModelAttribute = null;
                FieldInfo modelFieldInfo = null;
                DataContractAttribute dataContractAttribute;
                KeyPropertyAttribute modelKeyPropertyAttribute = null;
                PropertyInfo modelKeyProperty = null;
                DataMemberAttribute modelKeyDataMemberAttribute = null;

                if ((compositeDictionaryPropertyAttribute = compositeType.FindCustomAttribute<CompositeDictionaryPropertyAttribute>()) != null)
                {
                    var removedIdsProperty = compositeType
                        .GetProperty(compositeDictionaryPropertyAttribute.CompositeDictionaryPropertyName)
                        .GetValue(c)
                        .GetType().GetProperty(nameof(CompositeDictionary<object, Composite>.RemovedIds));

                    var compositeDictionary = compositeType
                        .GetProperty(compositeDictionaryPropertyAttribute.CompositeDictionaryPropertyName)
                        .GetValue(c);

                    if ((compositeModelAttribute = compositeType.FindCustomAttribute<CompositeModelAttribute>()) == null)
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.MustHaveCompositeModelAttribute, compositeType.Name));

                    if ((modelFieldInfo = compositeType.GetField(compositeModelAttribute.ModelFieldName, BindingFlags.Instance | BindingFlags.NonPublic)) == null)
                        throw new MemberAccessException(Resources.CannotFindCompositeModelProperty);

                    if ((dataContractAttribute = modelFieldInfo.FieldType.GetCustomAttribute<DataContractAttribute>()) == null)
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.MustHaveDataContractAttribute, modelFieldInfo.FieldType.Name));

                    if ((modelKeyPropertyAttribute = modelFieldInfo.FieldType.GetCustomAttribute<KeyPropertyAttribute>()) == null)
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.MustHaveKeyPropertyAttribute, modelFieldInfo.FieldType.Name));

                    if ((modelKeyProperty = modelFieldInfo.FieldType.GetProperty(modelKeyPropertyAttribute.PropertyName)) == null)
                        throw new InvalidOperationException();

                    if ((modelKeyDataMemberAttribute = modelKeyProperty.GetCustomAttribute<DataMemberAttribute>()) == null)
                        throw new InvalidOperationException();

                    var deletedIds = (IEnumerable<object>)removedIdsProperty.GetValue(compositeDictionary);

                    OnDelete(connection, transaction, dataContractAttribute.Name ?? modelFieldInfo.FieldType.Name, modelKeyDataMemberAttribute.Name ?? modelKeyProperty.Name, deletedIds);
                }

                switch (c.State)
                {
                    case CompositeState.Modified:
                        OnUpdate(connection, transaction, composite);
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

            var modelKeyPropertyName = string.Empty;
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

                modelKeyPropertyName = modelFieldInfo.FieldType.GetCustomAttribute<KeyPropertyAttribute>().PropertyName;
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

                dataTable.ExtendedProperties[nameof(SaveParameters.ModelKeyPropertyName)] = modelKeyPropertyName;
                dataTable.ExtendedProperties[nameof(SaveParameters.SqlColumnList)] = sqlColumnList;
                dataTable.ExtendedProperties[nameof(SaveParameters.SqlInsertColumnList)] = sqlInsertColumnList;

                dataTablesToInsert.Add(dataTable);
            }

            OnInsert(connection, transaction, dataTablesToInsert);
        }

        protected abstract DbConnection OnNewConnection(string connectionString);
        protected abstract DbTransaction OnNewTransaction(DbConnection connection);
        protected abstract void OnDelete(DbConnection connection, DbTransaction transaction, string tableName, string tableKeyPropertyName, IEnumerable<object> idValues);
        protected abstract void OnInsert(DbConnection connection, DbTransaction transaction, IReadOnlyList<DataTable> dataTablesToInsert);
        protected abstract void OnUpdate(DbConnection connection, DbTransaction transaction, Composite composite);
        protected abstract void OnCommit(DbConnection connection, DbTransaction transaction);
        protected abstract IEnumerable<T> OnLoad<T>(DbConnection connection, DbTransaction transaction, string query, IEnumerable<DbParameter> parameters) where T : new();
        protected abstract T OnExecute<T>(DbConnection connection, DbTransaction transaction, string statement, IEnumerable<DbParameter> parameters);
    }
}
