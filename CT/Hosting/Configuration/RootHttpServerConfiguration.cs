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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.Serialization;

namespace CT.Hosting.Configuration
{
    [DataContract]
    public class RootHttpServerConfiguration
    {
        public RootHttpServerConfiguration()
        {
            Initialize();
        }

        private void Initialize()
        {
            JsonSettings = new JsonSettings(this);
            rootConfigurations = new ConcurrentDictionary<Guid, RootConfiguration>();
            _rootConfigurations = new ReadOnlyDictionary<Guid, RootConfiguration>(rootConfigurations);
        }

        public RootHttpServerConfiguration(string endPoint, Type compositeRootType)
        {
            Initialize();

            var rootConfiguration = CreateNewRootConfiguration();
            rootConfiguration.Endpoint = endPoint;
            rootConfiguration.CompositeRootType = compositeRootType;
        }

        public RootHttpServerConfiguration(string endPoint, Type compositeRootType, CompositeRootMode mode)
        {
            Initialize();

            var rootConfiguration = CreateNewRootConfiguration();
            rootConfiguration.Endpoint = endPoint;
            rootConfiguration.CompositeRootType = compositeRootType;
            rootConfiguration.Mode = mode;
        }

        public RootHttpServerConfiguration(string endPoint, Type compositeRootType, CompositeRootMode mode, TimeSpan sessionExpiration)
        {
            Initialize();

            var rootConfiguration = CreateNewRootConfiguration();
            rootConfiguration.Endpoint = endPoint;
            rootConfiguration.CompositeRootType = compositeRootType;
            rootConfiguration.Mode = mode;
            rootConfiguration.SessionExpiration = sessionExpiration;

        }

        public RootHttpServerConfiguration(string endPoint, Type compositeRootType, CompositeRootMode mode, TimeSpan sessionExpiration, Type compositeRootAuthenticatorType)
        {
            Initialize();

            var rootConfiguration = CreateNewRootConfiguration();
            rootConfiguration.Endpoint = endPoint;
            rootConfiguration.CompositeRootType = compositeRootType;
            rootConfiguration.Mode = mode;
            rootConfiguration.SessionExpiration = sessionExpiration;
            rootConfiguration.CompositeRootAuthenticatorType = compositeRootAuthenticatorType;
        }

        public RootHttpServerConfiguration(string endPoint, Type compositeRootType, CompositeRootMode mode, TimeSpan sessionExpiration, Type compositeRootAuthenticatorType, string endPointPublicDirectory, string endPointPrivateDirectory)
        {
            Initialize();

            var rootConfiguration = CreateNewRootConfiguration();
            rootConfiguration.Endpoint = endPoint;
            rootConfiguration.CompositeRootType = compositeRootType;
            rootConfiguration.Mode = mode;
            rootConfiguration.SessionExpiration = sessionExpiration;
            rootConfiguration.CompositeRootAuthenticatorType = compositeRootAuthenticatorType;
            rootConfiguration.EndpointPublicDirectory = endPointPublicDirectory;
            rootConfiguration.EndpointPrivateDirectory = endPointPrivateDirectory;
        }

        public RootHttpServerConfiguration(string endPoint, Type compositeRootType, CompositeRootMode mode, TimeSpan sessionExpiration, Type compositeRootAuthenticatorType, string endPointPublicDirectory, string endPointPrivateDirectory, Type serverType)
        {
            Initialize();

            var rootConfiguration = CreateNewRootConfiguration();
            rootConfiguration.Endpoint = endPoint;
            rootConfiguration.CompositeRootType = compositeRootType;
            rootConfiguration.Mode = mode;
            rootConfiguration.SessionExpiration = sessionExpiration;
            rootConfiguration.CompositeRootAuthenticatorType = compositeRootAuthenticatorType;
            rootConfiguration.EndpointPublicDirectory = endPointPublicDirectory;
            rootConfiguration.EndpointPrivateDirectory = endPointPrivateDirectory;
            ServerType = serverType;
        }

        public RootHttpServerConfiguration(string endPoint, Type compositeRootType, CompositeRootMode mode, TimeSpan sessionExpiration, Type compositeRootAuthenticatorType, string endPointPublicDirectory, string endPointPrivateDirectory, Type serverType, TimeSpan serverBackgroundTaskInterval)
        {
            Initialize();

            var rootConfiguration = CreateNewRootConfiguration();
            rootConfiguration.Endpoint = endPoint;
            rootConfiguration.CompositeRootType = compositeRootType;
            rootConfiguration.Mode = mode;
            rootConfiguration.SessionExpiration = sessionExpiration;
            rootConfiguration.CompositeRootAuthenticatorType = compositeRootAuthenticatorType;
            rootConfiguration.EndpointPublicDirectory = endPointPublicDirectory;
            rootConfiguration.EndpointPrivateDirectory = endPointPrivateDirectory;
            ServerType = serverType;
            ServerBackgroundTaskInterval = serverBackgroundTaskInterval;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _rootConfigurations = new ReadOnlyDictionary<Guid, RootConfiguration>(rootConfigurations);
            this.RestoreParentReferences();
        }

        [DataMember]
        public TimeSpan ServerBackgroundTaskInterval { get; set; } = new TimeSpan(0, 0, 15);

        [DataMember]
        public JsonSettings JsonSettings { get; private set; }

        public Type ServerType { get; internal set; }
        [DataMember]
        public string ServerTypeName
        {
            get { return ServerType?.FullName; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.MustSupplyAValidTypeName, nameof(CompositeRootHttpServer)));

                var serverType = Type.GetType(value);

                if (serverType == null || (!serverType.IsSubclassOf(typeof(CompositeRootHttpServer)) && !serverType.IsAssignableFrom(typeof(CompositeRootHttpServer))))
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.MustBeOfType, value, nameof(CompositeRootHttpServer)));

                ServerType = serverType;
            }
        }

        [DataMember]
        internal ConcurrentDictionary<Guid, RootConfiguration> rootConfigurations;
        private ReadOnlyDictionary<Guid, RootConfiguration> _rootConfigurations;
        public IReadOnlyDictionary<Guid, RootConfiguration> RootConfigurations
        {
            get { return _rootConfigurations; }
        }

        public RootConfiguration CreateNewRootConfiguration()
        {
            return new RootConfiguration(this);
        }
    }
}
