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

using CT.Data.MicrosoftSqlServer;
using CT.Hosting.Configuration;
using CT.Blogs.Presentation.Properties;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using CT.Blogs.Presentation.BlogApplications.Blogs;
using System.IO;
using System;
using CT.Hosting;
using System.Linq;
using CT.Blogs.Presentation.BlogApplications.Users;
using System.Data.SqlClient;
using CT.Blogs.Model.Users;

namespace CT.Blogs.Presentation.BlogApplications
{
    [DataContract]
    public class BlogApplicationCompositeRoot : CompositeRoot
    {
        public BlogApplicationCompositeRoot(CompositeRootConfiguration configuration) : base(configuration)
        {
            Initialize();
        }

        public BlogApplicationCompositeRoot(CompositeRootConfiguration configuration, params IService[] services) : base(configuration, services)
        {
            Initialize();
        }

        public BlogApplicationCompositeRoot(CompositeRootConfiguration configuration, IEnumerable<Assembly> serviceAssemblies) : base(configuration, serviceAssemblies)
        {
            Initialize();
        }

        private void Initialize()
        {
            var machineName = Environment.MachineName;

            ConnectionString = Configuration.CustomSettings.TryGetValue($"{machineName}.MsSqlConnectionString", out string connectionString) ? connectionString : Configuration.CustomSettings["Local.MsSqlConnectionString"];
            MasterDbConnectionString = string.Format(ConnectionString, Configuration.CustomSettings.TryGetValue($"{machineName}.Database.Master", out string masterDbConnectionString) ? masterDbConnectionString : Configuration.CustomSettings["Local.Database.Master"]);
            BlogDbConnectionString = string.Format(ConnectionString, Configuration.CustomSettings.TryGetValue($"{machineName}.Database.BlogDb", out string blogDbConnectionString) ? blogDbConnectionString : Configuration.CustomSettings["Local.Database.BlogDb"]);

            AllBlogs = new BlogCompositeContainer(this);
        }

        internal string BlogDbConnectionString { get; private set; }
        internal string MasterDbConnectionString { get; private set; }
        internal string ConnectionString { get; private set; }

        public override void OnLogOn(CompositeRootHttpContext compositeRootHttpContext)
        {
            var blogApplication = CompositeRoot as BlogApplicationCompositeRoot;
            var repository = blogApplication.GetService<IMicrosoftSqlServerRepository>();
            var connectionString = blogApplication.BlogDbConnectionString;

            using (var connection = repository.OpenConnection(connectionString))
            {
                var user = repository.Load<User>(connection, null,
                            @"SELECT * 
                                FROM ""User"" 
                                WHERE Name = @userName
                                ",
                            new SqlParameter[]
                            {
                                    new SqlParameter("@userName", compositeRootHttpContext.Request.UserName)
                            }).FirstOrDefault();


                CurrentUser = new UserComposite(user) ?? null;
            }
            
        }

        [DataMember]
        [Help(typeof(Resources), nameof(Resources.BlogApplicationCompositeRoot_AllBlogs))]
        public BlogCompositeContainer AllBlogs { get; private set; }

        private string _errorMessage;
        [DataMember]
        [Help(typeof(Resources), nameof(Resources.BlogApplicationCompositeRoot_ErrorMessage))]
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set
            {
                _errorMessage = value;
                NotifyPropertyChanged(nameof(ErrorMessage));
            }
        }

        [Command]
        public void SetupDatabase(CompositeRootHttpContext context)
        {
            if (context.Request.UserName != "admin")
                throw new InvalidOperationException();

            var repository = GetService<IMicrosoftSqlServerRepository>();

            var createDatabaseSqlScriptFile = System.IO.Path.Combine(Environment.CurrentDirectory, "000-BlogServerDatabase.sql");

            using (var connection = repository.OpenConnection(MasterDbConnectionString))
            {
                var createDatabaseSql = File.ReadAllText(createDatabaseSqlScriptFile);
                repository.Execute<object>(connection, null, createDatabaseSql, null);
            }

            using (var connection = repository.OpenConnection(BlogDbConnectionString))
            using (var transaction = repository.BeginTransaction(connection))
            {
                repository.CreateHelperStoredProcedures(connection, transaction);
                repository.CommitTransaction(transaction);
            }

            using (var connection = repository.OpenConnection(BlogDbConnectionString))
            using (var transaction = repository.BeginTransaction(connection))
            {
                var directories = Directory
                    .GetDirectories(System.IO.Path.Combine(Environment.CurrentDirectory, "BlogApplications"), "", SearchOption.AllDirectories)
                    .GroupBy(d => new { Depth = d.Split(System.IO.Path.DirectorySeparatorChar).Count(), Directory = d })
                    .OrderBy(g => g.Key.Depth).ThenBy(g => g.Key.Directory)
                    .Select(g => g.Key.Directory);

                foreach(var directory  in directories)
                {
                    foreach(var sqlScriptFile in Directory.GetFiles(directory, "*.sql"))
                    {
                        var script = File.ReadAllText(sqlScriptFile);
                        repository.Execute<object>(connection, transaction, script, null);
                    }
                }

                repository.CommitTransaction(transaction);
            }
        }

        [DataMember]
        public UserComposite CurrentUser { get; private set; }
    }
}
