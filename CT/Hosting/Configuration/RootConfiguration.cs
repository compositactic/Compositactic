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
using System.Globalization;
using System.Runtime.Serialization;

namespace CT.Hosting.Configuration
{
    [DataContract]
    [ParentProperty(nameof(RootConfiguration.ServerConfiguration))]
    [KeyProperty(nameof(RootConfiguration.Id))]
    public class RootConfiguration
    {
        private RootConfiguration() { }

        internal RootConfiguration(RootHttpServerConfiguration serverConfiguration)
        {
            ServerConfiguration = serverConfiguration;
            ServerConfiguration.rootConfigurations.Load(this, _ => { return Guid.NewGuid(); });
        }

        public RootHttpServerConfiguration ServerConfiguration { get; internal set; }

        [OnDeserialized]
        private void Initialize(StreamingContext context)
        {
            if(Id == Guid.Empty)
                Id = Guid.NewGuid();
        }

        [DataMember]
        public Guid Id { get; private set; }

        private string endPoint;
        [DataMember]
        public string Endpoint
        {
            get { return endPoint; }
            set { endPoint = new Uri(value).ToStringClean(); }
        }

        private Type _compositeRootAuthenticatorType = typeof(AnonymousCompositeRootAuthenticator);
        public Type CompositeRootAuthenticatorType
        {
            get { return _compositeRootAuthenticatorType; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (!value.IsSubclassOf(typeof(CompositeRootAuthenticator)))
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.MustImplementInterface, value.Name, nameof(CompositeRootAuthenticator)));

                _compositeRootAuthenticatorType = value;
            }
        }

        [DataMember]
        public string CompositeRootAuthenticatorTypeName
        {
            get
            {
                return CompositeRootAuthenticatorType?.FullName;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.MustSupplyAValidTypeName, nameof(CompositeRootAuthenticator)));

                var compositeRootAuthenticatorType = Type.GetType(value);

                if (compositeRootAuthenticatorType == null || !compositeRootAuthenticatorType.IsSubclassOf(typeof(CompositeRootAuthenticator)))
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", nameof(CompositeRootAuthenticatorTypeName), value));

                _compositeRootAuthenticatorType = compositeRootAuthenticatorType;
            }
        }

        [DataMember]
        public string CompositeRootTypeName
        {
            get { return CompositeRootType?.FullName; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.MustSupplyAValidTypeName, nameof(CompositeRoot)));

                var compositeRootType = Type.GetType(value);

                if (compositeRootType == null || !compositeRootType.IsSubclassOf(typeof(CompositeRoot)))
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.MustBeOfType, value, nameof(CompositeRoot)));

                _compositeRootType = compositeRootType;
            }
        }

        private Type _compositeRootType;
        public Type CompositeRootType
        {
            get { return _compositeRootType; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (!value.IsSubclassOf(typeof(CompositeRoot)))
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.MustBeOfType, value.Name, nameof(CompositeRoot)));

                _compositeRootType = value;
            }
        }

        [DataMember]
        public TimeSpan SessionExpiration { get; set; } = TimeSpan.FromMinutes(20);

        [DataMember]
        public CompositeRootMode Mode { get; set; } = CompositeRootMode.MultipleHost;

        private static void ValidateDirectory(string directoryToValidate, string directoryToValidateWtih)
        {
            if (string.IsNullOrWhiteSpace(directoryToValidate) || System.IO.Path.IsPathRooted(directoryToValidate) || directoryToValidate.StartsWith("LOGON", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException(Resources.MustSupplyAValidRelativePath);

            var rootDirectory = directoryToValidate.Split(new char[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar })[0];

            if (!string.IsNullOrWhiteSpace(directoryToValidateWtih) && directoryToValidateWtih.StartsWith(rootDirectory))
                throw new ArgumentException(Resources.MustSupplyAValidRelativePath);
        }

        private string _endPointPrivateDirectory;
        [DataMember]
        public string EndpointPrivateDirectory
        {
            get { return _endPointPrivateDirectory; }
            set
            {
                ValidateDirectory(value, _endPointPublicDirectory);
                _endPointPrivateDirectory = value;
            }
        }

        private string _endPointPublicDirectory;
        [DataMember]
        public string EndpointPublicDirectory
        {
            get { return _endPointPublicDirectory; }
            set
            {
                ValidateDirectory(value, _endPointPrivateDirectory);
                _endPointPublicDirectory = value;
            }
        }

        [DataMember]
        public string PublicDirectoryHomeFile { get; set; }

        [DataMember]
        public Dictionary<string, string> CustomSettings { get; } = new Dictionary<string, string>();
    }
}
