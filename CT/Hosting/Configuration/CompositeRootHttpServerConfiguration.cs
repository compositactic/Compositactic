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
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;

namespace CT.Hosting.Configuration
{
    [DataContract]
    public class CompositeRootHttpServerConfiguration : Composite
    {
        private readonly BindingFlags _constructorBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        internal RootHttpServerConfiguration rootHttpServerConfiguration;

        internal CompositeRootHttpServerConfiguration(RootHttpServerConfiguration serverConfiguration)
        {
            rootHttpServerConfiguration = serverConfiguration;
            ServerRootConfigurations = new CompositeRootConfigurationContainer(this);
            JsonSettings = new CompositeJsonSettings(this);
        }

        public static CompositeRootHttpServerConfiguration Create(RootHttpServerConfiguration serverConfiguration)
        {
            return new CompositeRootHttpServerConfiguration(serverConfiguration);
        }

        public static CompositeRootHttpServerConfiguration Create(string endPoint, Type compositeRootType)
        {
            return new CompositeRootHttpServerConfiguration(new RootHttpServerConfiguration(endPoint, compositeRootType));
        }

        public static CompositeRootHttpServerConfiguration Create(string endPoint, Type compositeRootType, CompositeRootMode mode)
        {
            return new CompositeRootHttpServerConfiguration(new RootHttpServerConfiguration(endPoint, compositeRootType, mode));
        }

        public static CompositeRootHttpServerConfiguration Create(string endPoint, Type compositeRootType, CompositeRootMode mode, TimeSpan sessionExpiration)
        {
            return new CompositeRootHttpServerConfiguration(new RootHttpServerConfiguration(endPoint, compositeRootType, mode, sessionExpiration));
        }

        public static CompositeRootHttpServerConfiguration Create(string endPoint, Type compositeRootType, CompositeRootMode mode, TimeSpan sessionExpiration, Type compositeRootAuthenticatorType)
        {
            return new CompositeRootHttpServerConfiguration(new RootHttpServerConfiguration(endPoint, compositeRootType, mode, sessionExpiration, compositeRootAuthenticatorType));
        }

        public static CompositeRootHttpServerConfiguration Create(string endPoint, Type compositeRootType, CompositeRootMode mode, TimeSpan sessionExpiration, Type compositeRootAuthenticatorType, string endPointPublicDirectory, string endPointPrivateDirectory)
        {
            return new CompositeRootHttpServerConfiguration(new RootHttpServerConfiguration(endPoint, compositeRootType, mode, sessionExpiration, compositeRootAuthenticatorType, endPointPublicDirectory, endPointPrivateDirectory));
        }

        public static CompositeRootHttpServerConfiguration Create(string endPoint, Type compositeRootType, CompositeRootMode mode, TimeSpan sessionExpiration, Type compositeRootAuthenticatorType, string endPointPublicDirectory, string endPointPrivateDirectory, Type serverType)
        {
            return new CompositeRootHttpServerConfiguration(new RootHttpServerConfiguration(endPoint, compositeRootType, mode, sessionExpiration, compositeRootAuthenticatorType, endPointPublicDirectory, endPointPrivateDirectory, serverType));
        }

        public static CompositeRootHttpServerConfiguration Create(string endPoint, Type compositeRootType, CompositeRootMode mode, TimeSpan sessionExpiration, Type compositeRootAuthenticatorType, string endPointPublicDirectory, string endPointPrivateDirectory, Type serverType, TimeSpan serverBackgroundTaskInterval)
        {
            return new CompositeRootHttpServerConfiguration(new RootHttpServerConfiguration(endPoint, compositeRootType, mode, sessionExpiration, compositeRootAuthenticatorType, endPointPublicDirectory, endPointPrivateDirectory, serverType, serverBackgroundTaskInterval));
        }

        public CompositeRootHttpServer CreateServer(IEnumerable<Assembly> serviceAssemblies)
        {
            Server = rootHttpServerConfiguration.ServerType == null ? new CompositeRootHttpServer(this) : (CompositeRootHttpServer)Activator.CreateInstance(rootHttpServerConfiguration.ServerType, _constructorBindingFlags, null, new object[] { this, serviceAssemblies }, CultureInfo.InvariantCulture);
            return Server;
        }

        public CompositeRootHttpServer CreateServer(params IService[] services)
        {
            Server = rootHttpServerConfiguration.ServerType == null ? new CompositeRootHttpServer(this) : (CompositeRootHttpServer)Activator.CreateInstance(rootHttpServerConfiguration.ServerType, _constructorBindingFlags, null, new object[] { this, services }, CultureInfo.InvariantCulture);
            return Server;
        }

        public CompositeRootHttpServer CreateServer()
        {
            Server = rootHttpServerConfiguration.ServerType == null ? new CompositeRootHttpServer(this) : (CompositeRootHttpServer)Activator.CreateInstance(rootHttpServerConfiguration.ServerType, _constructorBindingFlags, null, new object[] { this }, CultureInfo.InvariantCulture);
            return Server;
        }

        [DataMember]
        public CompositeRootConfigurationContainer ServerRootConfigurations { get; internal set; }

        [DataMember]
        public CompositeRootHttpServer Server { get; internal set; }

        [DataMember]
        public string PhysicalPath {  get { return Environment.CurrentDirectory; } }

        [DataMember]
        public CompositeJsonSettings JsonSettings { get; set; }

        [DataMember]
        public TimeSpan ServerBackgroundTaskInterval
        {
            get { return rootHttpServerConfiguration.ServerBackgroundTaskInterval; }
            set
            {
                rootHttpServerConfiguration.ServerBackgroundTaskInterval = value;
                NotifyPropertyChanged(nameof(CompositeRootHttpServerConfiguration.ServerBackgroundTaskInterval));
            }
        }

        [DataMember]
        public string ServerTypeName
        {
            get { return rootHttpServerConfiguration.ServerTypeName; }
            set
            {
                rootHttpServerConfiguration.ServerTypeName = value;
                NotifyPropertyChanged(nameof(CompositeRootHttpServerConfiguration.ServerTypeName));
            }
        }
    }
}
