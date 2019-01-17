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

using CT.Hosting.Configuration;
using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Reflection;

namespace CT.Hosting
{
    [DataContract]
    [ParentProperty(nameof(CompositeRootContainer.Server))]
    [CompositeDictionaryProperty(nameof(CompositeRootContainer.CompositeRoots))]
    public class CompositeRootContainer : Composite
    {
        public CompositeRootHttpServer Server { get; }

        internal CompositeRootContainer(CompositeRootHttpServer server)
        {
            Server = server;
            compositeRoots = new CompositeDictionary<string, CompositeRoot>();
            CompositeRoots = new ReadOnlyCompositeDictionary<string, CompositeRoot>(compositeRoots);
        }

        internal CompositeDictionary<string, CompositeRoot> compositeRoots;
        [DataMember]
        public ReadOnlyCompositeDictionary<string, CompositeRoot> CompositeRoots { get; private set; }

        internal CompositeRoot CreateCompositeRoot(CompositeRootConfiguration configuration, EventHandler eventHandler, IEnumerable<IService> services)
        {
            var compositeRoot = CompositeRoot.Create(this, configuration, eventHandler, services);
            compositeRoot.ActiveCompositeRoots = this;
            compositeRoots.Add(configuration.Id.ToString(), compositeRoot);
            return compositeRoot;
        }

        internal CompositeRoot CreateCompositeRoot(CompositeRootConfiguration configuration, EventHandler eventHandler, IEnumerable<Assembly> serviceAssemblies)
        {
            var compositeRoot = CompositeRoot.Create(this, configuration, eventHandler, serviceAssemblies);
            compositeRoot.ActiveCompositeRoots = this;
            compositeRoots.Add(configuration.Id.ToString(), compositeRoot);
            return compositeRoot;
        }
    }
}
