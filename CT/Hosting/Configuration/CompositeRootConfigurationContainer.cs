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
using System.Runtime.Serialization;

namespace CT.Hosting.Configuration
{
    [DataContract]
    [ParentProperty(nameof(CompositeRootConfigurationContainer.ServerConfiguration))]
    [CompositeContainer(nameof(CompositeRootConfigurationContainer.RootConfigurations))]
    public class CompositeRootConfigurationContainer : Composite
    {
        internal CompositeRootConfigurationContainer(CompositeRootHttpServerConfiguration serverConfiguration)
        {
            ServerConfiguration = serverConfiguration;
            rootConfigurations = new CompositeDictionary<Guid, CompositeRootConfiguration>();
            RootConfigurations = new ReadOnlyCompositeDictionary<Guid, CompositeRootConfiguration>(rootConfigurations);

            foreach(var configuration in ServerConfiguration.rootHttpServerConfiguration.rootConfigurations.Values)
                rootConfigurations.Add(configuration.Id, new CompositeRootConfiguration(configuration, this));
        }

        public CompositeRootHttpServerConfiguration ServerConfiguration { get; private set; }


        internal CompositeDictionary<Guid, CompositeRootConfiguration> rootConfigurations;
        [DataMember]
        public ReadOnlyCompositeDictionary<Guid, CompositeRootConfiguration> RootConfigurations { get; private set; }

        [Command]
        public CompositeRootConfiguration CreateNewRootConfiguration(string endpoint, Type compositeRootType, Type compositeRootAuthenticatorType, CompositeRootMode mode, TimeSpan sessionExpiration)
        {
            if (mode == CompositeRootMode.None)
                throw new ArgumentException(string.Format(Resources.CompositeRootModeParameterCannotBeNone, nameof(mode), mode.ToString()), nameof(mode), null);

            var newRootConfiguration = new RootConfiguration(ServerConfiguration.rootHttpServerConfiguration)
            {
                Endpoint = endpoint,
                CompositeRootType = compositeRootType,
                CompositeRootAuthenticatorType = compositeRootAuthenticatorType,
                Mode = mode,
                SessionExpiration = sessionExpiration
            };

            var compositeRootConfiguration = new CompositeRootConfiguration(newRootConfiguration, this)
            {
                Endpoint = endpoint,
                CompositeRootType = compositeRootType,
                CompositeRootAuthenticatorType = compositeRootAuthenticatorType,
                Mode = mode,
                SessionExpiration = sessionExpiration
            };

            rootConfigurations.Add(compositeRootConfiguration.Id, compositeRootConfiguration);

            return compositeRootConfiguration;
        }
    }
}
