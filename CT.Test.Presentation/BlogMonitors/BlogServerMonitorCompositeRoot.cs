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

using CT.Blogs.Presentation.BlogApplications;
using CT.Hosting.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

namespace CT.Blogs.Presentation.BlogMonitors
{
    [DataContract]
    public class BlogServerMonitorCompositeRoot : CompositeRoot
    {
        public BlogServerMonitorCompositeRoot(CompositeRootConfiguration configuration) : base(configuration)
        {
            Initialize();
        }

        public BlogServerMonitorCompositeRoot(CompositeRootConfiguration configuration, params IService[] services) : base(configuration, services)
        {
            Initialize();
        }

        public BlogServerMonitorCompositeRoot(CompositeRootConfiguration configuration, IEnumerable<Assembly> serviceAssemblies) : base(configuration, serviceAssemblies)
        {
            Initialize();
        }

        private void Initialize()
        {
            var configurationFile = System.IO.Path.Combine(Environment.CurrentDirectory, "BlogServerConfig.json");
            var blogServerConfiguration = JsonConvert.DeserializeObject<RootHttpServerConfiguration>(File.ReadAllText(configurationFile));

            var serverConfiguration = CompositeRootHttpServerConfiguration.Create(blogServerConfiguration);

            BlogServer = serverConfiguration.CreateServer() as BlogApplicationServer;
            BlogServer.BlogMonitor = this;
            BlogServer.Start();
        }

        [DataMember]
        public BlogApplicationServer BlogServer { get; private set; }
    }
}
