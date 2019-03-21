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
            AllBlogs = new BlogCompositeContainer(this);
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
        public void Setup(CompositeRootHttpContext context)
        {
            if (context.Request.UserName != "admin")
                throw new InvalidOperationException();

            var repository = GetService<IMicrosoftSqlServerRepository>();
            var environment = Configuration.CustomSettings["Environment"];

            var connectionString = Configuration.CustomSettings[$"{environment}.MsSqlConnectionString"];
            var masterDbConnectionString = string.Format(connectionString, Configuration.CustomSettings[$"{environment}.Database.Master"]);
            var blogDbConnectionString = string.Format(connectionString, Configuration.CustomSettings[$"{environment}.Database.BlogDb"]);

            var createDatabaseSqlScriptFile = System.IO.Path.Combine(Environment.CurrentDirectory, "000-BlogServerDatabase.sql");
            var utilityScriptFile = System.IO.Path.Combine(Environment.CurrentDirectory, "001-Util.sql");

            using (var connection = repository.OpenConnection(masterDbConnectionString))
            using (var transaction = repository.BeginTransaction(connection))
            {
                var createDatabaseSql = File.ReadAllText(createDatabaseSqlScriptFile);
                repository.Execute<object>(connection, transaction, createDatabaseSql, null);
            }

            using (var connection = repository.OpenConnection(blogDbConnectionString))
            using (var transaction = repository.BeginTransaction(connection))
            {
                var utilitySql = File.ReadAllText(utilityScriptFile);
                repository.Execute<object>(connection, transaction, utilitySql, null);
            }
            
            //foreach(var scriptFile in Directory.GetFiles(Environment.CurrentDirectory).Except()

        }

        [Command]
        public void Save()
        {
            var repository = GetService<IMicrosoftSqlServerRepository>();

            var connectionString = Configuration.CustomSettings["ConnectionString"];

            using (var connection = repository.OpenConnection(connectionString))
            using (var transaction = repository.BeginTransaction(connection))
            {     
                repository.Save(connection, transaction, this);
                repository.CommitTransaction(transaction);
            }
        }
    }
}
