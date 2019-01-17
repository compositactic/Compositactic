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
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CT.Hosting
{
    [DataContract]
    [ParentProperty(nameof(CompositeRootSessionContainer.Server))]
    [CompositeDictionaryProperty(nameof(CompositeRootSessionContainer.Sessions))]
    public class CompositeRootSessionContainer : Composite
    {
        public CompositeRootHttpServer Server { get; private set; }
        private readonly IEnumerable<IService> _services;
        private readonly IEnumerable<Assembly> _serviceAssemblies;

        internal CompositeRootSessionContainer(CompositeRootHttpServer server, IEnumerable<IService> services)
        {
            Initialize(server);
            _services = services;
        }

        internal CompositeRootSessionContainer(CompositeRootHttpServer server, IEnumerable<Assembly> serviceAssemblies)
        {
            Initialize(server);
            _serviceAssemblies = serviceAssemblies;
        }

        private void Initialize(CompositeRootHttpServer server)
        {
            Server = server;
            sessions = new CompositeDictionary<string, CompositeRootSession>();
            Sessions = new ReadOnlyCompositeDictionary<string, CompositeRootSession>(sessions);
        }

        internal CompositeDictionary<string, CompositeRootSession> sessions;
        [DataMember]
        public ReadOnlyCompositeDictionary<string, CompositeRootSession> Sessions { get; private set; }

        [Command]
        public CompositeRootSession CreateNewCompositeRootSession(string endPoint, string userName, string token)
        {
            var compositeRootConfiguration = Server.ServerConfiguration.ServerRootConfigurations.RootConfigurations.Values.Single(c => string.Compare(c.Endpoint, endPoint, true) == 0);

            CompositeRoot compositeRoot = null;

            if (compositeRootConfiguration.Mode == CompositeRootMode.SingleHost)
                compositeRoot = Server.ActiveCompositeRoots.CompositeRoots.Values.Single(h => h.GetType() == compositeRootConfiguration.CompositeRootType);
            else
                compositeRoot = _serviceAssemblies != null ? 
                    CompositeRoot.Create(Server.ActiveCompositeRoots, compositeRootConfiguration, Server.CompositeRoot_EventAdded, _serviceAssemblies) :
                    CompositeRoot.Create(Server.ActiveCompositeRoots, compositeRootConfiguration, Server.CompositeRoot_EventAdded, _services);


            return CreateNewCompositeRootSession(endPoint, userName, token, compositeRootConfiguration.SessionExpiration, compositeRootConfiguration.Mode, compositeRoot);
        }

        public CompositeRootSession CreateNewCompositeRootSession(string endPoint, string userName, string token, TimeSpan sessionExpiration, CompositeRootMode mode, CompositeRoot compositeRoot)
        {
            var newCompositeRootSession = new CompositeRootSession(this)
            {
                lastAccessed = DateTime.Now,
                userName = userName,
                token = endPoint + token,
                expiration = sessionExpiration,
                mode = mode
            };

            sessions.Add(endPoint + token, newCompositeRootSession);
            compositeRoot.CompositeRootSession = newCompositeRootSession;

            if (mode == CompositeRootMode.MultipleHost)
            {
                compositeRoot.ActiveCompositeRoots = Server.ActiveCompositeRoots;
                Server.ActiveCompositeRoots.compositeRoots.dictionary.TryAdd(endPoint + token, compositeRoot);
            }

            return newCompositeRootSession;
        }
    }
}
