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
using CT.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace CT.Hosting.Test
{
    public class CompositeRootHttpServerTester : IDisposable
    {
        public CompositeRootHttpServer Server { get; } = null;

        private readonly ConcurrentDictionary<string, CompositeRootHttpServerTesterConnection> _connections = new ConcurrentDictionary<string, CompositeRootHttpServerTesterConnection>();
        public IReadOnlyDictionary<string, CompositeRootHttpServerTesterConnection> Connections {  get { return _connections; } }

        public CompositeRootHttpServerConfiguration Configuration { get; private set; }

        public CompositeRootHttpServerTester(Type compositeRootHttpServerType, Type compositeRootType, Type compositeRootAuthenticatorType)
        {
            var serverConfiguration = Setup(compositeRootHttpServerType, compositeRootType, compositeRootAuthenticatorType);
            Server = serverConfiguration.CreateServer();
        }

        public CompositeRootHttpServerTester(Type compositeRootHttpServerType, Type compositeRootType, Type compositeRootAuthenticatorType, params IService[] services)
        {
            var serverConfiguration = Setup(compositeRootHttpServerType, compositeRootType, compositeRootAuthenticatorType);
            Server = serverConfiguration.CreateServer(services);
        }

        public CompositeRootHttpServerTester(Type compositeRootHttpServerType, Type compositeRootType, Type compositeRootAuthenticatorType, IEnumerable<Assembly> serviceAssemblies)
        {
            var serverConfiguration = Setup(compositeRootHttpServerType, compositeRootType, compositeRootAuthenticatorType);
            Server = serverConfiguration.CreateServer(serviceAssemblies);
        }

        public CompositeRootHttpServerTester(RootHttpServerConfiguration rootHttpServerConfiguration, params IService[] services)
        {
            var serverConfiguration = Setup(rootHttpServerConfiguration);
            Server = serverConfiguration.CreateServer(services);
        }

        public CompositeRootHttpServerTester(RootHttpServerConfiguration rootHttpServerConfiguration, IEnumerable<Assembly> serviceAssemblies)
        {
            var serverConfiguration = Setup(rootHttpServerConfiguration);
            Server = serverConfiguration.CreateServer(serviceAssemblies);
        }

        private CompositeRootHttpServerConfiguration Setup(RootHttpServerConfiguration rootHttpServerConfiguration)
        {
            if (rootHttpServerConfiguration == null)
                throw new ArgumentNullException(nameof(rootHttpServerConfiguration));
            
            foreach(var rootConfiguration in rootHttpServerConfiguration.RootConfigurations.Values)
            {
                if (string.IsNullOrEmpty(rootConfiguration.Endpoint))
                    rootConfiguration.Endpoint = string.Format(CultureInfo.InvariantCulture, "http://{0}:{1}/{2}/", IPAddress.Loopback.ToString(), GetFreePortNumber(), GetType().Name);
            }

            var serverConfiguration = new CompositeRootHttpServerConfiguration(rootHttpServerConfiguration);
            Configuration = serverConfiguration;
            return serverConfiguration;
        }

        private CompositeRootHttpServerConfiguration Setup(Type compositeRootHttpServerType, Type compositeRootType, Type compositeRootAuthenticatorType)
        {
            if (compositeRootHttpServerType == null)
                throw new ArgumentNullException(nameof(compositeRootHttpServerType));

            if (compositeRootType == null)
                throw new ArgumentNullException(nameof(compositeRootType));

            if (compositeRootAuthenticatorType == null)
                throw new ArgumentNullException(nameof(compositeRootAuthenticatorType));

            if (!compositeRootHttpServerType.IsAssignableFrom(typeof(CompositeRootHttpServer)) && !compositeRootHttpServerType.IsSubclassOf(typeof(CompositeRootHttpServer)))
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.MustBeOfType, compositeRootHttpServerType.Name, nameof(CompositeRootHttpServer)));

            var port = GetFreePortNumber();

            var serverConfiguration = new CompositeRootHttpServerConfiguration(new RootHttpServerConfiguration { ServerType = compositeRootHttpServerType })
            {
                ServerBackgroundTaskInterval = TimeSpan.FromSeconds(15),
                ServerTypeName = compositeRootHttpServerType.AssemblyQualifiedName
            };

            serverConfiguration.ServerRootConfigurations.CreateNewRootConfiguration(string.Format(CultureInfo.InvariantCulture, "http://{0}:{1}/{2}/", IPAddress.Loopback.ToString(), port, GetType().Name),
                                                compositeRootType, compositeRootAuthenticatorType, CompositeRootMode.MultipleHost, TimeSpan.FromMinutes(20));
            Configuration = serverConfiguration;
            return serverConfiguration;
        }

        private static int GetFreePortNumber()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            return port;
        }

        public CompositeRootHttpServerTester(CompositeRootHttpServerConfiguration configuration)
        {
            Server = configuration.CreateServer();
            Configuration = configuration;
        }

        public void Initialize()
        {
            Server.ServerConfiguration = Configuration;
            Server.Start();
        }

        public static HttpWebRequest CreateLogOnWebRequest(CompositeRootConfiguration configuration, string parameters)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            return (HttpWebRequest)WebRequest.Create((new Uri(configuration.Endpoint + nameof(CompositeRootAuthenticator.LogOn) + "?" + parameters)));
        }

        public static HttpWebRequest CreateRequest(CompositeRootConfiguration configuration, string path, string sessionToken)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            return (HttpWebRequest)WebRequest.Create(new Uri(configuration.Endpoint + sessionToken + "/" + path));
        }

        public static HttpWebRequest CreateRequest(CompositeRootConfiguration configuration, string sessionToken)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            return (HttpWebRequest)WebRequest.Create(new Uri(configuration.Endpoint + sessionToken));
        }

        public CompositeRootHttpServerTesterConnection LogOnUser(CompositeRootConfiguration configuration, WebRequest request)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            return CreateCompositeRootHttpServerTesterConnection(configuration, request);
        }

        public CompositeRootHttpServerTesterConnection LogOnUser(CompositeRootConfiguration configuration, string parameters)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            return CreateCompositeRootHttpServerTesterConnection(configuration, CreateLogOnWebRequest(configuration, parameters));
        }

        private CompositeRootHttpServerTesterConnection CreateCompositeRootHttpServerTesterConnection(CompositeRootConfiguration configuration, WebRequest request)
        {
            var response = JsonConvert.DeserializeObject<CompositeRootAuthenticatorResponse>(Encoding.UTF8.GetString(CompositeRootHttpServerTesterConnection.SendRequest(request, out _, out _)));

            var sessionToken = response.SessionToken;
            CompositeRootHttpServerTesterConnection compositeRootHttpServerTesterConnection = null;
            try
            {
                compositeRootHttpServerTesterConnection = new CompositeRootHttpServerTesterConnection(configuration.Endpoint, sessionToken);
                if (!_connections.TryAdd(sessionToken, compositeRootHttpServerTesterConnection))
                    return null;
            }
            catch
            {
                compositeRootHttpServerTesterConnection?.Dispose();
                throw;
            }

            return compositeRootHttpServerTesterConnection;
        }

        private static IEnumerable<KeyValuePair<string, string>> GetFormFields(Uri commandPath)
        {
            if (!string.IsNullOrEmpty(commandPath.Query))
            {
                var formFields = new List<KeyValuePair<string, string>>();
                var parameters = commandPath.Query.Substring(1).Split('&');
                foreach (var parameter in parameters)
                {
                    if (parameter.Split('=').Length < 2)
                        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", CommandRequestError.MissingParameterValue, parameter));

                    formFields.Add(new KeyValuePair<string, string>(parameter.Split('=')[0], parameter.Split('=')[1]));
                }
                return formFields;
            }

            return new KeyValuePair<string, string>[] { };
        }

        public static IEnumerable<CompositeRootCommandResponse> SendRequest(CompositeRootConfiguration configuration, string command, IEnumerable<FileInfo> files, string sessionToken)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var commandPath = new Uri("file:///" + command);
            var formFields = GetFormFields(commandPath);
           
            var returnedBytes = CompositeRootHttpServerTesterConnection.SendMultipartFormDataRequest(
                                                                        configuration.Endpoint + sessionToken + commandPath.AbsolutePath,
                                                                        files.Select(f => f.FullName).ToArray(),
                                                                        formFields, out string responseContentType, out string responseContentEncoding);
            try
            {
                return JsonConvert.DeserializeObject<CompositeRootCommandResponse[]>(Encoding.UTF8.GetString(returnedBytes)) as IEnumerable<CompositeRootCommandResponse>;
            }
            catch
            {
                return new CompositeRootCommandResponse[]
                {
                    new CompositeRootCommandResponse
                    {
                        Success = true,
                        ReturnValue = returnedBytes,
                        ReturnValueContentType = responseContentType,
                        ReturnValueContentEncoding = responseContentEncoding
                    }
                };
            }
        }

        public static IEnumerable<CompositeRootCommandResponse> SendRequest(WebRequest request, IEnumerable<FileInfo> files)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var formFields = GetFormFields(request.RequestUri);

            var returnedBytes = CompositeRootHttpServerTesterConnection.SendMultipartFormDataRequest(
                                                            request,
                                                            files.Select(f => f.FullName).ToArray(),
                                                            formFields, out string responseContentType, out string responseContentEncoding);
            try
            {
                return JsonConvert.DeserializeObject<CompositeRootCommandResponse[]>(Encoding.UTF8.GetString(returnedBytes)) as IEnumerable<CompositeRootCommandResponse>;
            }
            catch
            {
                return new CompositeRootCommandResponse[]
                {
                    new CompositeRootCommandResponse
                    {
                        Success = true,
                        ReturnValue = returnedBytes,
                        ReturnValueContentType = responseContentType,
                        ReturnValueContentEncoding = responseContentEncoding
                    }
                };
            }
        }

        public static IEnumerable<CompositeRootCommandResponse> SendRequest(CompositeRootConfiguration configuration, string command, string sessionToken)
        {
            return SendRequest(configuration, new CompositeRootCommandRequest[] { CompositeRootCommandRequest.Create(1, command) }, sessionToken);
        }

        public static IEnumerable<CompositeRootCommandResponse> SendRequest(WebRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return SendRequest(request, new CompositeRootCommandRequest[] { CompositeRootCommandRequest.Create(1, request.RequestUri.AbsolutePath.Trim('/','\\')) });
        }

        public static IEnumerable<CompositeRootCommandResponse> SendRequest(CompositeRootConfiguration configuration, IEnumerable<CompositeRootCommandRequest> commands, string sessionToken)
        {
            if (commands == null)
                throw new ArgumentNullException(nameof(commands));

            return GetCommandResponses(CreateRequest(configuration, sessionToken), commands);
        }

        public static IEnumerable<CompositeRootCommandResponse> SendRequest(WebRequest request, IEnumerable<CompositeRootCommandRequest> commands)
        {
            if (commands == null)
                throw new ArgumentNullException(nameof(commands));

            return GetCommandResponses(request, commands);
        }

        private static IEnumerable<CompositeRootCommandResponse> GetCommandResponses(WebRequest request, IEnumerable<CompositeRootCommandRequest> commands)
        {
            using (var requestContentStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(commands))))
            {
                var returnedBytes = CompositeRootHttpServerTesterConnection.SendRequest(request, "application/json", requestContentStream, out string responseContentType, out string responseContentEncoding);

                try
                {
                    return JsonConvert.DeserializeObject<CompositeRootCommandResponse[]>(Encoding.UTF8.GetString(returnedBytes)) as IEnumerable<CompositeRootCommandResponse>;
                }
                catch
                {
                    return new CompositeRootCommandResponse[]
                    {
                        new CompositeRootCommandResponse
                        {
                            Success = true,
                            ReturnValue = returnedBytes,
                            ReturnValueContentType = responseContentType,
                            ReturnValueContentEncoding = responseContentEncoding
                        }
                    };
                }
            }
        }

        bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                foreach (var connection in _connections.Values)
                    connection.Dispose();
            }

            disposed = true;
        }

        ~CompositeRootHttpServerTester()
        {
            Dispose(false);
        }
    }
}
