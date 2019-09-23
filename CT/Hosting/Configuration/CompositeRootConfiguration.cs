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
using System.Runtime.Serialization;

namespace CT.Hosting.Configuration
{
    [DataContract(Namespace = "")]
    [KeyProperty(nameof(CompositeRootConfiguration.Id))]
    [ParentProperty(nameof(CompositeRootConfiguration.ActiveRootConfigurations))]
    public class CompositeRootConfiguration : Composite
    {
        private readonly RootConfiguration _rootConfiguration;

        internal CompositeRootConfiguration(RootConfiguration rootConfiguration, CompositeRootConfigurationContainer compositeRootConfigurationContainer)
        {
            _rootConfiguration = rootConfiguration;
            ActiveRootConfigurations = compositeRootConfigurationContainer;
            CompositeRootAuthenticatorType = _rootConfiguration.CompositeRootAuthenticatorType;
        }

        public CompositeRootConfigurationContainer ActiveRootConfigurations { get; internal set; }

        [DataMember]
        public Guid Id { get; private set; } = Guid.NewGuid();

        [DataMember]
        public string Endpoint
        {
            get { return _rootConfiguration.Endpoint; }
            set
            {
                _rootConfiguration.Endpoint = value;
                NotifyPropertyChanged(nameof(CompositeRootConfiguration.Endpoint));
            }
        }

        [DataMember]
        public string CompositeRootAuthenticatorTypeName
        {
            get { return _rootConfiguration.CompositeRootAuthenticatorTypeName; }
            set
            {
                _rootConfiguration.CompositeRootAuthenticatorTypeName = value;
                NotifyPropertyChanged(nameof(CompositeRootConfiguration.CompositeRootAuthenticatorTypeName));
            }
        }
        
        public Type CompositeRootAuthenticatorType
        {
            get { return _rootConfiguration.CompositeRootAuthenticatorType; }
            set { _rootConfiguration.CompositeRootAuthenticatorType = value; }
        }

        [DataMember]
        public string CompositeRootTypeName
        {
            get { return _rootConfiguration.CompositeRootTypeName; }
            set
            {
                _rootConfiguration.CompositeRootTypeName = value;
                NotifyPropertyChanged(nameof(CompositeRootConfiguration.CompositeRootTypeName));
            }
        }
        
        public Type CompositeRootType
        {
            get { return _rootConfiguration.CompositeRootType; }
            set { _rootConfiguration.CompositeRootType = value; }
        }

        [DataMember]
        public TimeSpan SessionExpiration
        {
            get { return _rootConfiguration.SessionExpiration; }
            set
            {
                _rootConfiguration.SessionExpiration = value;
                NotifyPropertyChanged(nameof(CompositeRootConfiguration.SessionExpiration));
            }
        }

        [DataMember]
        public CompositeRootMode Mode
        {
            get { return _rootConfiguration.Mode; }
            set
            {
                if(ActiveRootConfigurations.ServerConfiguration.Server == null || 
                    ActiveRootConfigurations.ServerConfiguration.Server.Status == CompositeRootHttpServerStatus.Stopped)
                    _rootConfiguration.Mode = value;

                NotifyPropertyChanged(nameof(CompositeRootConfiguration.Mode));
            } 
        }

        [DataMember]
        public string EndpointPrivateDirectory
        {
            get { return _rootConfiguration.EndpointPrivateDirectory; }
            set
            {
                _rootConfiguration.EndpointPrivateDirectory = value;
                NotifyPropertyChanged(nameof(CompositeRootConfiguration.EndpointPrivateDirectory));
            }
        }

        [DataMember]
        public string EndpointPublicDirectory
        {
            get { return _rootConfiguration.EndpointPublicDirectory; }
            set
            {
                _rootConfiguration.EndpointPublicDirectory = value;
                NotifyPropertyChanged(nameof(CompositeRootConfiguration.EndpointPublicDirectory));
            }
        }

        [DataMember]
        public string PublicDirectoryHomeFile
        {
            get { return _rootConfiguration.PublicDirectoryHomeFile; }
            set
            {
                _rootConfiguration.PublicDirectoryHomeFile = value;
                NotifyPropertyChanged(nameof(CompositeRootConfiguration.PublicDirectoryHomeFile));
            }
        }

        [DataMember]
        public Dictionary<string, string> CustomSettings { get { return _rootConfiguration.CustomSettings; } }

        [Command]
        public void Remove()
        {
            ActiveRootConfigurations.rootConfigurations.Remove(Id);
        }
    }
}
