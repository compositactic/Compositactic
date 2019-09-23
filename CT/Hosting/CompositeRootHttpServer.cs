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
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace CT.Hosting
{
    [DataContract]
    public class CompositeRootHttpServer : Composite, IDisposable
    {
        protected CompositeRootHttpServer() { }

        protected internal CompositeRootHttpServer(CompositeRootHttpServerConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            Initialize(configuration);
        }

        private readonly IEnumerable<IService> _services;
        private readonly IEnumerable<Assembly> _serviceAssemblies;

        protected internal CompositeRootHttpServer(CompositeRootHttpServerConfiguration configuration, IEnumerable<IService> services)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            Initialize(configuration, services);
            _services = services;
        }

        protected internal CompositeRootHttpServer(CompositeRootHttpServerConfiguration configuration, IEnumerable<Assembly> serviceAssemblies)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            Initialize(configuration, serviceAssemblies);
            _serviceAssemblies = serviceAssemblies;
        }

        public CompositeRootHttpServerConfiguration ServerConfiguration { get; internal set; }

        [DataMember]
        public CompositeRootContainer ActiveCompositeRoots { get; private set; }

        [DataMember]
        public CompositeRootSessionContainer ActiveSessions { get; private set; }

        private Dictionary<string, CompositeRootConfiguration> _configurations;

        [NonSerialized]
        private Dictionary<string, CompositeCommandLogEntry> _logOnLog;

        [NonSerialized]
        private readonly object _logOnLogLock = new object();

        private void Initialize(CompositeRootHttpServerConfiguration configuration)
        {
            InitializeHttpListener(configuration);

            try
            {
                foreach (var config in configuration.ServerRootConfigurations.RootConfigurations.Values)
                {
                    SetupConfiguration(config, (IEnumerable<IService>)null);
                    OnAfterConfiguration(config);
                }
            }
            catch (ArgumentException e)
            {
                OnConfigurationError(e.Data["configuration"] as CompositeRootConfiguration, e.Message);
                throw e;
            }
        }

        private void Initialize(CompositeRootHttpServerConfiguration configuration, IEnumerable<Assembly> serviceAssemblies)
        {
            InitializeHttpListener(configuration, serviceAssemblies);

            try
            {
                foreach (var config in configuration.ServerRootConfigurations.RootConfigurations.Values)
                {
                    SetupConfiguration(config, serviceAssemblies);
                    OnAfterConfiguration(config);
                }
            }
            catch (ArgumentException e)
            {
                OnConfigurationError(e.Data["configuration"] as CompositeRootConfiguration, e.Message);
                throw e;
            }
        }

        private void Initialize(CompositeRootHttpServerConfiguration configuration, IEnumerable<IService> services)
        {
            InitializeHttpListener(configuration, services);

            try
            {
                foreach (var config in configuration.ServerRootConfigurations.RootConfigurations.Values)
                {
                    SetupConfiguration(config, services);
                    OnAfterConfiguration(config);
                }
            }
            catch (ArgumentException e)
            {
                OnConfigurationError(e.Data["configuration"] as CompositeRootConfiguration, e.Message);
                throw e;
            }
        }

        private void InitializeHttpListener(CompositeRootHttpServerConfiguration configuration)
        {
            CreateHttpListener(configuration);
            ActiveSessions = new CompositeRootSessionContainer(this, (IEnumerable<IService>)null);
        }

        private void InitializeHttpListener(CompositeRootHttpServerConfiguration configuration, IEnumerable<IService> services)
        {
            CreateHttpListener(configuration);
            ActiveSessions = new CompositeRootSessionContainer(this, services);
        }

        private void InitializeHttpListener(CompositeRootHttpServerConfiguration configuration, IEnumerable<Assembly> serviceAssemblies)
        {
            CreateHttpListener(configuration);
            ActiveSessions = new CompositeRootSessionContainer(this, serviceAssemblies);
        }

        private void CreateHttpListener(CompositeRootHttpServerConfiguration configuration)
        {
            configuration.Server = this;
            ServerConfiguration = configuration;
            ServerConfiguration.Server = this;
            _configurations = new Dictionary<string, CompositeRootConfiguration>();
            ActiveCompositeRoots = new CompositeRootContainer(this);

            _httpListener = new HttpListener
            {
                IgnoreWriteExceptions = true
            };

            _logOnLog = new Dictionary<string, CompositeCommandLogEntry>();

            HttpListenerProperties = new HttpListenerProperties(_httpListener);
        }

        protected virtual void OnAfterConfiguration(CompositeRootConfiguration configuration)
        {
        }

        protected virtual void OnConfigurationError(CompositeRootConfiguration configuration, string errorMessage)
        {
        }

        [NonSerialized]
        private EventHandler _shutdownComplete = null;
        public event EventHandler ShutdownComplete
        {
            add { _shutdownComplete += value; }
            remove { _shutdownComplete -= value; }
        }

        [NonSerialized]
        private HttpListener _httpListener;

        private void SetupConfiguration(CompositeRootConfiguration configuration, IEnumerable<IService> services)
        {
            AddNewHttpListenerPrefix(configuration);
            if (configuration.Mode == CompositeRootMode.SingleHost)
                ActiveCompositeRoots.CreateCompositeRoot(configuration, CompositeRoot_EventAdded, services);
        }

        private void SetupConfiguration(CompositeRootConfiguration configuration, IEnumerable<Assembly> serviceAssemblies)
        {
            AddNewHttpListenerPrefix(configuration);
            if (configuration.Mode == CompositeRootMode.SingleHost)
                ActiveCompositeRoots.CreateCompositeRoot(configuration, CompositeRoot_EventAdded, serviceAssemblies);
        }

        private void AddNewHttpListenerPrefix(CompositeRootConfiguration configuration)
        {
            if(!string.IsNullOrWhiteSpace(configuration.EndpointPublicDirectory))
            {
                var endpointPublicDirectory = System.IO.Path.Combine(configuration.ActiveRootConfigurations.ServerConfiguration.PhysicalPath, configuration.EndpointPublicDirectory);
                if (!Directory.Exists(endpointPublicDirectory))
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.DirectoryDoesNotExist, endpointPublicDirectory));
            }

            if(!string.IsNullOrWhiteSpace(configuration.EndpointPrivateDirectory))
            {
                var endpointPrivateDirectory = System.IO.Path.Combine(configuration.ActiveRootConfigurations.ServerConfiguration.PhysicalPath, configuration.EndpointPrivateDirectory);
                if (!Directory.Exists(endpointPrivateDirectory))
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.DirectoryDoesNotExist, endpointPrivateDirectory));
            }

            if (string.IsNullOrEmpty(configuration.Endpoint))
                throw new ArgumentException(Resources.MustSupplyValidEndpoint);

            _configurations.Add(configuration.Endpoint, configuration);

            try
            {
                _httpListener.Prefixes.Add(configuration.Endpoint);
            }
            catch (ArgumentException e)
            {
                e.Data["configuration"] = configuration;
                throw e;
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(ServerConfiguration.ServerRootConfigurations.RootConfigurations.Values.Any(c => c.Mode == CompositeRootMode.MultipleHost))
                LogOffExpiredHosts();
            Task.Run(() => { OnBackgroundTask(); });
        }

        private async void LogOffExpiredHosts()
        {
            await Task.Run(() =>
            {
                RemoveExpiredCompositeRoots();
            });
        }

        private void RemoveExpiredCompositeRoots()
        {
            var sessionTokensToExpire = ActiveCompositeRoots.CompositeRoots
                .Keys
                .Where(sessionToken =>
                            ActiveSessions.Sessions.ContainsKey(sessionToken) &&
                            DateTime.Now - ActiveSessions.Sessions[sessionToken].LastAccessed > 
                                            ActiveCompositeRoots.CompositeRoots[sessionToken].CompositeRootSession.Expiration).ToList();

            sessionTokensToExpire.ForEach((sessionToken) =>
            {
                var compositeRoot = ActiveCompositeRoots.CompositeRoots[sessionToken];
                var compositeRootSession = ActiveSessions.Sessions[sessionToken];
                if (ActiveCompositeRoots.CompositeRoots[sessionToken].CompositeRootSession.Mode == CompositeRootMode.MultipleHost)
                {
                    compositeRoot.DoCompositeRootLogOffTask();
                    compositeRootSession.EventsDone.WaitOne();

                    ActiveCompositeRoots.compositeRoots.Remove(sessionToken, out CompositeRoot removedCompositeRoot);
                }

                ActiveSessions.sessions.Remove(sessionToken);

                _logOnLog.Remove(_logOnLog.Values.SingleOrDefault(c => c.Session.Token == sessionToken).RequestId);

                compositeRootSession.Dispose();
            });

        }

        protected virtual void OnBackgroundTask()
        {
        }

        [NonSerialized]
        private Timer _backgroundTaskTimer = null;
        private void StartBackgroundTaskTimer(TimeSpan backgroundTaskInterval)
        {
            try
            {
                _backgroundTaskTimer = new Timer(backgroundTaskInterval.TotalMilliseconds)
                {
                    AutoReset = true
                };

                _backgroundTaskTimer.Elapsed += Timer_Elapsed;
                _backgroundTaskTimer.Start();
            }
            catch
            {
                _backgroundTaskTimer?.Dispose();
                throw;
            }
        }

        private CompositeRootHttpServerStatus _status = CompositeRootHttpServerStatus.Stopped;
        [DataMember]
        public CompositeRootHttpServerStatus Status
        {
            get { return _status; }
            internal set
            {
                _status = value;
                NotifyPropertyChanged(nameof(CompositeRootHttpServer.Status));
            }
        }

        [Command] 
        public void Start()
        {
            if(Status == CompositeRootHttpServerStatus.Stopped)
            {
                try
                {
                    _httpListener.Start();
                }
                catch(ObjectDisposedException)
                {
                    if (_serviceAssemblies != null)
                        Initialize(ServerConfiguration, _serviceAssemblies);
                    else if (_services != null)
                        Initialize(ServerConfiguration, _services);
                    else
                        Initialize(ServerConfiguration);

                    _httpListener.Start();
                }

                StartBackgroundTaskTimer(ServerConfiguration.ServerBackgroundTaskInterval);
                Status = CompositeRootHttpServerStatus.Running;
                ProcessRequests();
            }
        }

        [Command]
        public void Stop()
        { 
            if(Status == CompositeRootHttpServerStatus.Running)
            {
                foreach (var session in ActiveSessions.Sessions.Values)
                    session.LastAccessed = DateTime.MinValue;

                RemoveExpiredCompositeRoots();
                _httpListener.Close();
                _backgroundTaskTimer.Stop();
                Status = CompositeRootHttpServerStatus.Stopped;
            }
        }

        private async void ProcessRequests()
        {
            var requestTasks = new List<Task>();

            for (int i = 0; i < Environment.ProcessorCount; i++)
                requestTasks.Add(_httpListener.GetContextAsync());

            do
            {
                var listenTask = await Task.WhenAny(requestTasks);
                requestTasks.Remove(listenTask);

                if (!(listenTask is Task<HttpListenerContext>))
                    continue;

                HttpListenerContext context;

                try
                {
                    context = (listenTask as Task<HttpListenerContext>).Result;
                }
                catch(Exception)
                {
                    break;
                }

                var requestUrl = context.Request.Url;
                Task requestTask = null;
                var filePath = string.Empty;

                var compositeRootConfiguration = GetConfiguration(requestUrl);
                var sessionToken = requestUrl.ToStringClean().Replace(compositeRootConfiguration.Endpoint, string.Empty).Split('/')[0];
                var alternateSourcedSessionToken = OnGetSessionTokenFromRequest(context, compositeRootConfiguration);

                if (sessionToken.ToUpperInvariant() == "FAVICON.ICO")
                    requestTask = Task.Run(() => { WriteFile(context, new FileInfo(System.IO.Path.Combine(compositeRootConfiguration.ActiveRootConfigurations.ServerConfiguration.PhysicalPath, compositeRootConfiguration.EndpointPublicDirectory, "favicon.ico")), null, null, compositeRootConfiguration); });
                else 
                    if (sessionToken.ToUpperInvariant() == compositeRootConfiguration.EndpointPublicDirectory?.ToUpperInvariant() &&
                                                                    requestUrl.IsRequestForPublicFile(compositeRootConfiguration, ref filePath))
                        requestTask = Task.Run(() => { WriteFile(context, new FileInfo(System.IO.Path.Combine(compositeRootConfiguration.ActiveRootConfigurations.ServerConfiguration.PhysicalPath, filePath)), null, null, compositeRootConfiguration); });
                else if (requestUrl.IsRequestForLogin(compositeRootConfiguration))
                {
                    requestTask = Task.Run(() => { LogOn(context, compositeRootConfiguration); });
                }
                else
                {
                    CompositeRoot compositeRoot = null;

                    if (Regex.IsMatch(requestUrl.OriginalString, "^" + compositeRootConfiguration.Endpoint + "*?$", RegexOptions.IgnoreCase) && string.IsNullOrEmpty(alternateSourcedSessionToken))
                        requestTask = Task.Run(() => { WriteHomeFile(context, compositeRootConfiguration); });
                    else
                    {
                        try
                        {
                            compositeRoot = GetCompositeRoot(alternateSourcedSessionToken ?? sessionToken, compositeRootConfiguration);
                        }
                        catch (Exception e)
                        {
                            requestTask = Task.Run(() => { WriteResponse(context, compositeRootConfiguration, GetExceptionResponse(e), alternateSourcedSessionToken ?? sessionToken); });
                        }
                    }

                    if (compositeRoot != null)
                    {
                        if (requestUrl.IsRequestForEvents(compositeRootConfiguration))
                        {
                            var eventThread = new System.Threading.Thread(() => { WriteEvents(context, compositeRootConfiguration, compositeRoot); });
                            eventThread.Start();
                        }
                        else if (requestUrl.IsRequestForPrivateFile(compositeRootConfiguration, compositeRoot, ref filePath))
                            requestTask = Task.Run(() => { WriteFile(context, new FileInfo(System.IO.Path.Combine(compositeRootConfiguration.ActiveRootConfigurations.ServerConfiguration.PhysicalPath, filePath)), compositeRoot, alternateSourcedSessionToken ?? sessionToken, compositeRootConfiguration); });
                        else
                            requestTask = Task.Run(() => { ExecuteCommandRequests(context, compositeRoot, alternateSourcedSessionToken, sessionToken, compositeRootConfiguration); });
                    }
                }

                listenTask.Dispose();

                if(requestTask != null)
                    requestTasks.Add(requestTask);

                requestTasks.Add(_httpListener.GetContextAsync());

            } while (true);
        }

        protected virtual string OnGetSessionTokenFromRequest(HttpListenerContext httpListenerContext, CompositeRootConfiguration compositeRootConfiguration)
        {
            return null;
        }

        private static IEnumerable<CompositeRootCommandResponse> GetExceptionResponse(Exception e)
        {
            return new CompositeRootCommandResponse[]
            {
                new CompositeRootCommandResponse
                {
                    Success = false,
                    Errors = GetErrorMessages(e)
                }
            };
        }

        private CompositeRootConfiguration GetConfiguration(Uri uri)
        {
            var url = uri.ToStringClean() + "/";
            var key = _configurations.Keys.Where(k => url.StartsWith(k)).OrderByDescending(m => m.Length).FirstOrDefault();
            if (key == null)
                return null;

            return _configurations[key];
        }

        private CompositeRoot GetCompositeRoot(string sessionToken, CompositeRootConfiguration compositeRootConfiguration)
        {
            CompositeRoot compositeRoot;
            var key = compositeRootConfiguration.Endpoint + sessionToken;

            if (!ActiveSessions.Sessions.TryGetValue(key, out CompositeRootSession compositeRootSession))
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", CommandRequestError.InvalidSessionToken.ToString(), sessionToken));

            compositeRoot = compositeRootSession.Mode == CompositeRootMode.SingleHost ?
                                    ActiveCompositeRoots.CompositeRoots[compositeRootConfiguration.Id.ToString()] :
                                    ActiveCompositeRoots.CompositeRoots[key];

            ActiveSessions.Sessions[key].LastAccessed = DateTime.Now;

            return compositeRoot;
        }

        private static string GetRequest(HttpListenerContext context, CultureInfo cultureInfo, out IEnumerable<CompositeUploadedFile> uploadedFiles, out IEnumerable<CompositeRootCommandRequest> multipleCommandRequest, out string requestId)
        {
            multipleCommandRequest = null;
            var uploadedFilesList = new List<CompositeUploadedFile>();
            var request = context.Request;

            var requestBody = string.Empty;
            if (request.InputStream != Stream.Null)
            {
                byte[] requestContent;
                string requestContentType;
                Encoding requestEncoding;

                using (var requestStream = new MemoryStream())
                {
                    request.InputStream.CopyTo(requestStream);
                    requestEncoding = request.ContentEncoding;
                    requestContentType = string.IsNullOrEmpty(request.ContentType) ? "application/x-www-form-urlencoded" : request.ContentType;
                    requestContent = requestStream.ToArray();
                }

                Match matchedBoundary;

                if ((matchedBoundary = Regex.Match(requestContentType, @"^multipart/form-data;\s+boundary=(?'boundary'.+)$")).Success)
                    requestBody = GetMultiPartFormDataRequest(uploadedFilesList, requestContent, requestEncoding, matchedBoundary.Groups["boundary"].Value);
                else if (Regex.IsMatch(requestContentType, @"application/x-www-form-urlencoded|application/json"))
                {
                    requestBody = requestEncoding.GetString(requestContent);
                    try
                    {
                        multipleCommandRequest = JsonConvert.DeserializeObject<IEnumerable<CompositeRootCommandRequest>>(requestBody, new JsonSerializerSettings { Culture = cultureInfo });
                    }
                    catch (JsonReaderException)
                    {
                    }
                }
                else
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", CommandRequestError.RequestContentTypeNotSupported, request.ContentType));
            }
            else if(!string.IsNullOrEmpty(request.Url.Query))
                requestBody = request.Url.Query.Substring(1);

            uploadedFiles = uploadedFilesList;

            requestId = context.Request.Headers["X-Request-ID"] ?? Regex.Match(requestBody, @"x-request-id=(?'xrequestid'[^&]*)", RegexOptions.IgnoreCase).Groups["xrequestid"].Value;

            return requestBody;
        }

        private static string GetMultiPartFormDataRequest(List<CompositeUploadedFile> uploadedFilesList, byte[] requestContent, Encoding requestEncoding, string boundry)
        {
            var headerEndBytes = Encoding.ASCII.GetBytes("\r\n\r\n");
            var boundaryBytes = requestEncoding.GetBytes("--" + boundry);
            var boundaryIndexes = FindBytes(requestContent, boundaryBytes);

            var requestBodyBuilder = new StringBuilder();

            for (var i = 0; i < boundaryIndexes.Count; i++)
            {
                var startIndex = boundaryIndexes[i] + boundaryBytes.Length;
                var endIndex = i + 1 == boundaryIndexes.Count ? requestContent.Length - 1 : boundaryIndexes[i + 1] - 1;

                var blockBytes = requestContent.Skip(startIndex).Take((endIndex - startIndex) - 1).ToArray();
                var contentBeginIndex = FindBytes(blockBytes, headerEndBytes).FirstOrDefault();

                if (contentBeginIndex == 0)
                    continue;

                var headerText = requestEncoding.GetString(blockBytes.Take(contentBeginIndex).ToArray());

                Match nameMatch;
                Match contentTypeMatch;

                if ((nameMatch = Regex.Match(headerText, @"Content-Disposition: form-data; name=\x22(?'name'\w+)\x22;?\s*(?:filename=\x22(?'filename'[^\x22]+)\x22)?")).Success)
                {
                    if ((contentTypeMatch = Regex.Match(headerText, @"Content-Type:\s+(?'contentType'\S+\w)")).Success)
                        uploadedFilesList.Add(new CompositeUploadedFile(nameMatch.Groups["name"].Value, nameMatch.Groups["filename"].Value, blockBytes.Skip(contentBeginIndex + 4).ToArray(), contentTypeMatch.Groups["contentType"].Value));
                    else
                    {
                        var formDataValue = requestEncoding.GetString(blockBytes.Skip(contentBeginIndex + 4).ToArray());
                        requestBodyBuilder.AppendFormat(CultureInfo.CurrentCulture, "{0}={1}&", nameMatch.Groups["name"].Value, formDataValue == "%00" ? "%00" : Uri.EscapeDataString(formDataValue));
                    }
                }
            }

            return requestBodyBuilder.ToString();
        }

        private static IReadOnlyList<int> FindBytes(byte[] buffer, byte[] pattern)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            var positions = new List<int>();

            if (buffer.Length < pattern.Length)
                return positions;

            for (var bufferIndex = 0; bufferIndex < buffer.Length - pattern.Length + 1; bufferIndex++)
                if (!pattern.Where((data, index) => !buffer[bufferIndex + index].Equals(data)).Any())
                    positions.Add(bufferIndex);

            return positions;
        }

        protected virtual RequestProcessingAction OnBeforeExecuteCommandRequests(HttpListenerContext httpListenerContext, CompositeRoot compositeRoot, string sessionToken, IEnumerable<CompositeRootCommandRequest> commandRequests, IEnumerable<CompositeUploadedFile> uploadedFiles)
        {
            return RequestProcessingAction.Continue;
        }

        private void ExecuteCommandRequests(HttpListenerContext context, CompositeRoot compositeRoot, string alternateSourcedSessionToken, string sessionToken, CompositeRootConfiguration compositeRootConfiguration)
        {
            var requestUrl = context.Request.Url;
            var requestBody = GetRequest(context, context.Request.UserLanguages.GetCultureInfo(), out IEnumerable<CompositeUploadedFile> uploadedFiles, out IEnumerable<CompositeRootCommandRequest> multipleCommandRequests, out string requestId);
            var activeSession = ActiveSessions.Sessions[compositeRootConfiguration.Endpoint + sessionToken];

            var commandRequests = multipleCommandRequests ??
                                        new CompositeRootCommandRequest[]
                                        {
                                            new CompositeRootCommandRequest(1)
                                            {
                                                CommandPath = string.IsNullOrEmpty(alternateSourcedSessionToken) ?
                                                                Regex.Replace(requestUrl.ToString(), @"^" + compositeRootConfiguration.Endpoint + "*" + sessionToken + "/*", string.Empty) :
                                                                Regex.Replace(requestUrl.ToString(), @"^" + compositeRootConfiguration.Endpoint + "*", string.Empty)
                                            }
                                        };

            if (OnBeforeExecuteCommandRequests(context, compositeRoot, sessionToken, commandRequests, uploadedFiles) == RequestProcessingAction.Stop)
            {
                context.Response.Close();
                return;
            }

            var commandResponses = new List<CompositeRootCommandResponse>();

            CompositeRootHttpContext compositeRootHttpContext = null;
            var returnValue = new object();
            CompositeCommandLogEntry compositeCommandLogEntry = null;

            lock(activeSession.commandLogLock)
            {
                try
                {
                    if (string.IsNullOrEmpty(requestId))
                        throw new ArgumentNullException(Resources.MustSupplyRequestId);

                    if (activeSession.commandLog.ContainsKey(requestId))
                        compositeCommandLogEntry = activeSession.commandLog[requestId];
                    else
                    {
                        foreach (var commandRequest in commandRequests)
                        {
                            var commandResponseReturnValuePlaceholderMatches = Regex.Matches(commandRequest.CommandPath, @"{(?'commandId'\d+)/?(?'path'.+?)?}").Cast<Match>();

                            foreach (var commandResponseReturnValuePlaceholderMatch in commandResponseReturnValuePlaceholderMatches)
                            {
                                var commandResponseReturnValue = commandResponses.Single(cr => cr.Id == int.Parse(commandResponseReturnValuePlaceholderMatch.Groups["commandId"].Value, CultureInfo.InvariantCulture)).ReturnValue;
                                var commandResponseReturnValuePath = commandResponseReturnValuePlaceholderMatch.Groups["path"].Value;
                                var commandResponseReturnValueComposite = commandResponseReturnValue as Composite;
                                var returnValuePlaceholder = commandRequest.CommandPath.Substring(commandResponseReturnValuePlaceholderMatch.Index, commandResponseReturnValuePlaceholderMatch.Length);
                                if (commandResponseReturnValueComposite != null && !string.IsNullOrEmpty(commandResponseReturnValuePath))
                                {
                                    var valueForPlaceholder = commandResponseReturnValueComposite.Execute(commandResponseReturnValuePath, context, string.Empty, sessionToken, uploadedFiles);
                                    if (!valueForPlaceholder.ReturnValue.GetType().IsConvertable())
                                        throw new ArgumentException(
                                            string.Format(CultureInfo.CurrentCulture, Resources.PlaceholderValueConversionError,
                                                                    valueForPlaceholder.ReturnValue.GetType().FullName,
                                                                    nameof(System.ComponentModel.TypeConverter),
                                                                    nameof(String)));

                                    commandRequest.CommandPath = commandRequest.CommandPath.Replace(returnValuePlaceholder, valueForPlaceholder.ReturnValue.ToString());
                                }
                                else if (commandResponseReturnValueComposite != null && string.IsNullOrEmpty(commandResponseReturnValuePath))
                                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.PlaceholderPathRequiredError, commandResponseReturnValueComposite.GetType().FullName));
                                else
                                    commandRequest.CommandPath = commandRequest.CommandPath.Replace(returnValuePlaceholder, commandResponseReturnValue.ToString());
                            }

                            var commandResponse = compositeRoot.Execute(commandRequest.CommandPath, context, activeSession.UserName, sessionToken, uploadedFiles);
                            compositeRootHttpContext = commandResponse.Context;
                            returnValue = commandResponse.ReturnValue;
                            commandResponses.Add(new CompositeRootCommandResponse { Id = commandRequest.Id, Success = true, ReturnValue = returnValue, ReturnValueContentType = compositeRootHttpContext?.Response.ContentType });
                        }

                        compositeCommandLogEntry = new CompositeCommandLogEntry(requestId, activeSession, returnValue is byte[]? returnValue : commandResponses);
                        activeSession.commandLog.Add(requestId, compositeCommandLogEntry);
                    }
                }
                catch (Exception e)
                {
                    compositeCommandLogEntry = new CompositeCommandLogEntry(requestId, activeSession, returnValue is byte[]? returnValue : commandResponses);
                    commandResponses.Add(new CompositeRootCommandResponse { Success = false, Errors = GetErrorMessages(e) });
                }
                finally
                {
                    if(!compositeCommandLogEntry.IsWritten)
                    {
                        SetHttpListenerResponse(compositeRootHttpContext, context.Response);
                        OnAfterExecuteCommandRequests(context, compositeRootConfiguration, returnValue, commandResponses, sessionToken);
                        compositeCommandLogEntry.IsWritten = WriteResponse(context, compositeRootConfiguration, compositeCommandLogEntry.Response, sessionToken);
                        compositeCommandLogEntry.Response = compositeCommandLogEntry.IsWritten ? null : compositeCommandLogEntry.Response;
                    }
                }
            }
        }

        protected virtual void OnAfterExecuteCommandRequests(HttpListenerContext httpListenerContext, CompositeRootConfiguration compositeRootConfiguration, object returnValue, IEnumerable<CompositeRootCommandResponse> commandResponses, string sessionToken)
        {
        }

        protected virtual RequestProcessingAction OnBeforeLogOn(HttpListenerContext httpListenerContext, CompositeRootConfiguration compositeRootConfiguration, string requestParameters, IEnumerable<CompositeUploadedFile> uploadedFiles)
        {
            return RequestProcessingAction.Continue;
        }

        private void LogOn(HttpListenerContext context, CompositeRootConfiguration compositeRootConfiguration)
        {
            var requestBody = GetRequest(context, context.Request.UserLanguages.GetCultureInfo(), out IEnumerable<CompositeUploadedFile> uploadedFiles, out IEnumerable<CompositeRootCommandRequest> dummy, out string requestId);

            CompositeRootHttpContext compositeRootHttpContext = null;
            CompositeRootSession compositeRootSession = null;
            CompositeRootAuthenticatorResponse authenticatorResponse = null;
            CompositeCommandLogEntry compositeCommandLogEntry = null;

            var userName = Regex.Match(requestBody, @"username=(?'username'[^&]*)", RegexOptions.IgnoreCase).Groups["username"]?.Value ?? string.Empty;

            lock(_logOnLogLock)
            {
                try
                {
                    if (string.IsNullOrEmpty(requestId))
                        throw new ArgumentNullException(Resources.MustSupplyRequestId);

                    if (_logOnLog.ContainsKey(requestId))
                        compositeCommandLogEntry = _logOnLog[requestId];
                    else
                    {
                        if (OnBeforeLogOn(context, compositeRootConfiguration, requestBody, uploadedFiles) == RequestProcessingAction.Stop)
                        {
                            context.Response.Close();
                            return;
                        }

                        CompositeRoot compositeRoot = null;

                        if (compositeRootConfiguration.Mode == CompositeRootMode.SingleHost)
                            compositeRoot = ActiveCompositeRoots.CompositeRoots.Values.Single(h => h.GetType() == compositeRootConfiguration.CompositeRootType);
                        else
                            compositeRoot = CompositeRoot.Create(compositeRootConfiguration, CompositeRoot_EventAdded, _services);

                        var loginResponse = compositeRoot.Authenticator.Execute(nameof(CompositeRootAuthenticator.LogOn) + "?" + requestBody, context, userName, string.Empty, uploadedFiles);
                        compositeRootHttpContext = loginResponse.Context;
                        authenticatorResponse = loginResponse.ReturnValue as CompositeRootAuthenticatorResponse;

                        compositeCommandLogEntry = new CompositeCommandLogEntry(requestId, null, authenticatorResponse);

                        if (authenticatorResponse.IsAuthenticationSuccessful)
                            compositeRootSession = ActiveSessions.CreateNewCompositeRootSession(compositeRootConfiguration.Endpoint, authenticatorResponse.UserName, authenticatorResponse.SessionToken, compositeRootConfiguration.SessionExpiration, compositeRootConfiguration.Mode, compositeRoot);

                        compositeRoot.OnLogOn(compositeRootHttpContext);

                        compositeCommandLogEntry.Session = compositeRootSession;
                        _logOnLog.Add(requestId, compositeCommandLogEntry);
                    }
                }
                catch (Exception e)
                {
                    compositeRootSession = null;
                    compositeRootSession?.Dispose();
                    compositeCommandLogEntry = new CompositeCommandLogEntry(requestId, null, new CompositeRootCommandResponse { Success = false, Errors = GetErrorMessages(e) });
                }
                finally
                {
                    if (!compositeCommandLogEntry.IsWritten)
                    {
                        SetHttpListenerResponse(compositeRootHttpContext, context.Response);
                        OnAfterLogOn(context, compositeRootConfiguration, requestBody, uploadedFiles, authenticatorResponse);
                        compositeCommandLogEntry.IsWritten = WriteResponse(context, compositeRootConfiguration, compositeCommandLogEntry.Response, compositeRootSession?.Token);
                        compositeCommandLogEntry.Response = compositeCommandLogEntry.IsWritten ? null : compositeCommandLogEntry.Response;
                    }
                }
            }
        }

        protected virtual void OnAfterLogOn(HttpListenerContext httpListenerContext, CompositeRootConfiguration compositeRootConfiguration, string requestParameters, IEnumerable<CompositeUploadedFile> uploadedFiles, CompositeRootAuthenticatorResponse authenticatorResponse)
        {
        }

        private void WriteHomeFile(HttpListenerContext httpListenerContext, CompositeRootConfiguration compositeRootConfiguration)
        {
            if (httpListenerContext == null)
                throw new ArgumentNullException(nameof(httpListenerContext));

            if (compositeRootConfiguration == null)
                throw new ArgumentNullException(nameof(compositeRootConfiguration));

            var response = httpListenerContext.Response;

            if (!string.IsNullOrWhiteSpace(compositeRootConfiguration.PublicDirectoryHomeFile))
            {
                var url = new Uri(httpListenerContext.Request.Url.ToString() + "/" + compositeRootConfiguration.EndpointPublicDirectory + "/" + compositeRootConfiguration.PublicDirectoryHomeFile);
                response.StatusCode = (int)HttpStatusCode.Redirect;
                response.Headers.Add(HttpResponseHeader.Location, url.ToString());
            }
            else
            {
                response.ContentEncoding = httpListenerContext.Request.ContentEncoding;

                var testPageBytes = response.ContentEncoding.GetBytes(Resources.defaultHomeFile
                                                                        .RunTemplate("context", new FileRequestContext
                                                                        {
                                                                            CompositeRootConfiguration = compositeRootConfiguration,
                                                                            HttpListenerContext = httpListenerContext
                                                                        }, @"<!--\s*ct:", @"-->"));
                response.ContentLength64 = testPageBytes.Length;
                response.ContentType = "text/html";
                using (var writer = new StreamWriter(response.OutputStream, response.ContentEncoding))
                {
                    OnWriteHomeFile(httpListenerContext, compositeRootConfiguration, writer.BaseStream, testPageBytes);
                }
            }

            response.Close();
        }

        protected virtual void OnWriteHomeFile(HttpListenerContext httpListenerContext, CompositeRootConfiguration compositeRootConfiguration, Stream responseStream, byte[] content)
        {
            if (responseStream == null)
                throw new ArgumentNullException(nameof(responseStream));

            if (content == null)
                throw new ArgumentNullException(nameof(content));

            responseStream.Write(content, 0, content.Length);
        }

        protected virtual RequestProcessingAction OnBeforeWriteFile(HttpListenerContext httpListenerContext, FileInfo fileInfo, CompositeRoot compositeRoot, string sessionToken)
        {
            return RequestProcessingAction.Continue;
        }

        private void WriteFile(HttpListenerContext httpListenerContext, FileInfo fileInfo, CompositeRoot compositeRoot, string sessionToken, CompositeRootConfiguration compositeRootConfiguration)
        {
            if (httpListenerContext == null)
                throw new ArgumentNullException(nameof(httpListenerContext));
            
            if (fileInfo == null)
                throw new ArgumentNullException(nameof(fileInfo));

            var response = httpListenerContext.Response;

            if (OnBeforeWriteFile(httpListenerContext, fileInfo, compositeRoot, sessionToken) == RequestProcessingAction.Stop)
            {
                response.Close();
                return;
            }

            var requestUrl = httpListenerContext.Request.Url.ToString();
            if (Regex.IsMatch(requestUrl, @"[\/\\]$"))
            {
                response.StatusCode = (int)HttpStatusCode.MovedPermanently;
                response.Headers.Add(HttpResponseHeader.Location, requestUrl.TrimEnd('/', '\\'));
            }
            else
            {
                var requestHeaders = (WebHeaderCollection)httpListenerContext.Request.Headers;
                var shouldCompress = requestHeaders[HttpRequestHeader.AcceptEncoding] != null ? requestHeaders[HttpRequestHeader.AcceptEncoding].Contains("gzip") : false;
                if (shouldCompress)
                    httpListenerContext.Response.Headers[HttpResponseHeader.ContentEncoding] = "gzip";

                var eTag = requestHeaders[HttpRequestHeader.IfNoneMatch];
                if (eTag == fileInfo.LastWriteTime.Ticks.ToString(CultureInfo.InvariantCulture))
                    response.StatusCode = (int)HttpStatusCode.NotModified;
                else
                {
                    using (var contentStream = new MemoryStream())
                    {
                        using (var stream = shouldCompress ? (Stream)new GZipStream(contentStream, CompressionLevel.Fastest) : contentStream)
                        {
                            if (!fileInfo.Exists)
                            {
                                response.StatusCode = (int)HttpStatusCode.NotFound;
                                response.ContentType = "text/html";
                                var notFoundPageContent = Encoding.UTF8.GetBytes(
                                    Resources.defaultNotFoundPage.RunTemplate("context",
                                    new FileRequestContext
                                    {
                                        CompositeRootSession = ActiveSessions.Sessions.ContainsKey(compositeRootConfiguration.Endpoint + sessionToken) ?
                                                                        ActiveSessions.Sessions[compositeRootConfiguration.Endpoint + sessionToken] : null,
                                        CompositeRoot = compositeRoot,
                                        FileInfo = fileInfo,
                                        CompositeRootConfiguration = compositeRootConfiguration,
                                        HttpListenerContext = httpListenerContext
                                    }, @"<!--\s*ct:", @"-->"));
                                stream.Write(notFoundPageContent, 0, notFoundPageContent.Length);
                            }
                            else
                            {
                                response.ContentType = ContentTypes.GetContentTypeFromFileExtension(fileInfo.Extension);
                                response.Headers.Add(HttpResponseHeader.ETag, fileInfo.LastWriteTime.Ticks.ToString(CultureInfo.InvariantCulture));

                                if (requestHeaders[HttpRequestHeader.Range] != null)
                                {
                                    var partialContentBoundary = "--" + Guid.NewGuid().ToString();

                                    response.Headers.Add(HttpResponseHeader.AcceptRanges, "bytes");
                                    response.StatusCode = (int)HttpStatusCode.PartialContent;

                                    var requestedRanges = requestHeaders[HttpRequestHeader.Range].Split(',');

                                    if (requestedRanges.Length > 1)
                                        httpListenerContext.Response.Headers[HttpResponseHeader.ContentType] = "multipart/byteranges; boundary=" + partialContentBoundary;

                                    var ranges = new List<Tuple<long?, long?>>();

                                    for (int rangeIndex = 0; rangeIndex < requestedRanges.Length; rangeIndex++)
                                    {
                                        var range = requestedRanges[rangeIndex];

                                        var m = Regex.Match(range, @"(bytes=)?(?'bytesRangeBegin'\d*)(?'bytesRangeEnd'-\d*)");
                                        if (m.Success)
                                        {
                                            var bytesRangeBegin = long.TryParse(m.Groups["bytesRangeBegin"].Value, out long rangeValue) ? rangeValue : (long?)null;
                                            var bytesRangeEnd = long.TryParse(m.Groups["bytesRangeEnd"].Value, out rangeValue) ? rangeValue : (long?)null;
                                            ranges.Add(new Tuple<long?, long?>(bytesRangeBegin, bytesRangeEnd));
                                        }
                                    }

                                    var fileSeekOffsetsAndLengths = GetFileSeekOffsetsAndLengths(fileInfo, ranges).ToArray();
                                    if (requestedRanges.Length == fileSeekOffsetsAndLengths.Length)
                                    {
                                        for (int rangeIndex = 0; rangeIndex < fileSeekOffsetsAndLengths.Length; rangeIndex++)
                                            OnWriteFile(httpListenerContext,
                                                        compositeRoot,
                                                        fileInfo,
                                                        stream,
                                                        sessionToken,
                                                        fileSeekOffsetsAndLengths[rangeIndex].Item1,
                                                        fileSeekOffsetsAndLengths[rangeIndex].Item2,
                                                        true,
                                                        requestedRanges.Length == 1,
                                                        partialContentBoundary,
                                                        rangeIndex == requestedRanges.Length - 1);
                                    }
                                    else
                                    {
                                        response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
                                        response.Headers.Clear();
                                        response.Headers.Add(HttpResponseHeader.ContentRange, "bytes */" + (fileInfo.Length - 1).ToString(CultureInfo.InvariantCulture));
                                    }
                                }
                                else
                                    OnWriteFile(httpListenerContext, compositeRoot, fileInfo, stream, sessionToken, 0, fileInfo.Length, false, false, null, true);
                            }
                        }

                        using (var contentStreamCopy = new MemoryStream(contentStream.ToArray()))
                        {
                            response.ContentLength64 = contentStreamCopy.Length;
                            contentStreamCopy.CopyTo(response.OutputStream);
                        }
                    }
                }
            }

            OnAfterWriteFile(httpListenerContext, fileInfo, compositeRoot, sessionToken);
            response.Close();
        }

        private IEnumerable<Tuple<long, long>> GetFileSeekOffsetsAndLengths(FileInfo fileInfo, IEnumerable<Tuple<long?, long?>> ranges)
        {
            foreach (var range in ranges)
            {
                Tuple<long, long> fileOffsetAndLength;
                if (range.Item1.HasValue && range.Item1.Value > 0 && range.Item1.Value < fileInfo.Length - 1)
                    fileOffsetAndLength = new Tuple<long, long>(range.Item1.Value, (range.Item2 == null ? fileInfo.Length - 1 : -range.Item2.Value) - range.Item1.Value + 1);
                else if (range.Item2.HasValue && -range.Item2.Value > 0 && -range.Item2.Value < fileInfo.Length)
                    fileOffsetAndLength = new Tuple<long, long>((fileInfo.Length - 1) - (-range.Item2.Value - 1), -range.Item2.Value);
                else
                    yield break;

                yield return fileOffsetAndLength;
            }
        }

        protected virtual void OnWriteFile(HttpListenerContext httpListenerContext,
                                            CompositeRoot compositeRoot,
                                            FileInfo requestedFileInfo,
                                            Stream outputStream,
                                            string sessionToken,
                                            long fileOffsetFromBeginning,
                                            long fileReadLength,
                                            bool isPartialContent,
                                            bool isPartialContentSingleRange,
                                            string partialContentBoundary,
                                            bool isLastRequestedRange)
        {
            if (outputStream == null)
                throw new ArgumentNullException(nameof(outputStream));

            if (requestedFileInfo == null)
                throw new ArgumentNullException(nameof(requestedFileInfo));
            
            if (httpListenerContext == null)
                throw new ArgumentNullException(nameof(httpListenerContext));

            var fileContentType = ContentTypes.GetContentTypeFromFileExtension(requestedFileInfo.Extension);

            using (var fileStream = File.OpenRead(requestedFileInfo.FullName))
            {
                fileStream.Seek(fileOffsetFromBeginning, SeekOrigin.Begin);

                if (isPartialContent && !isPartialContentSingleRange)
                {
                    var rangeHeaderContent = Encoding.UTF8.GetBytes(partialContentBoundary + "\r\n" +
                                                "Content-Type: " + fileContentType + "\r\n" +
                                                "Content-Range: bytes " + fileStream.Position + "-" + ((fileStream.Position + fileReadLength) - 1) + "/" + requestedFileInfo.Length + "\r\n\r\n");

                    outputStream.Write(rangeHeaderContent, 0, rangeHeaderContent.Length);
                }

                var buffer = new byte[4096];
                var bytesRead = 0;
                var totalBytesRead = 0L;
                while (totalBytesRead < fileReadLength)
                {
                    bytesRead = fileStream.Read(buffer, 0, Math.Min(buffer.Length, (int)(fileReadLength % (int.MaxValue + 1L))));
                    outputStream.Write(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                }

                var boundaryBytes = (isPartialContent && isLastRequestedRange && !isPartialContentSingleRange) ? 
                                                    Encoding.UTF8.GetBytes("\r\n" + partialContentBoundary) : 
                                                    Encoding.UTF8.GetBytes("\r\n");

                outputStream.Write(boundaryBytes, 0, boundaryBytes.Length);
            }
        }

        protected virtual void OnAfterWriteFile(HttpListenerContext httpListenerContext, FileInfo fileInfo, CompositeRoot compositeRoot, string sessionToken)
        {
        }

        protected virtual RequestProcessingAction OnBeforeWriteResponse(HttpListenerContext httpListenerContext, CompositeRootConfiguration compositeRootConfiguration, object value, string sessionToken)
        {
            return RequestProcessingAction.Continue;
        }

        private bool WriteResponse(HttpListenerContext httpListenerContext, CompositeRootConfiguration compositeRootConfiguration, object value, string sessionToken)
        {
            if (httpListenerContext == null)
                throw new ArgumentNullException(nameof(httpListenerContext));

            var response = httpListenerContext.Response;

            if (OnBeforeWriteResponse(httpListenerContext, compositeRootConfiguration, value, sessionToken) == RequestProcessingAction.Stop)
            {
                response.Close();
                return true;
            }

            bool wroteResponse;
            try
            {
                if (value is byte[] binaryResponse)
                {
                    response.ContentEncoding = httpListenerContext.Request.ContentEncoding;
                    response.ContentLength64 = binaryResponse.LongLength;
                    using (var writer = new BinaryWriter(response.OutputStream, response.ContentEncoding))
                    {
                        OnWriteResponse(httpListenerContext, compositeRootConfiguration, value, writer, binaryResponse, sessionToken);
                    }
                }
                else
                {
                    var responseContent = JsonConvert.SerializeObject(value, compositeRootConfiguration.ActiveRootConfigurations.ServerConfiguration.JsonSettings.jsonSettings.jsonSerializerSettings);

                    response.ContentEncoding = httpListenerContext.Request.ContentEncoding;
                    response.ContentLength64 = response.ContentEncoding.GetByteCount(responseContent);
                    response.ContentType = "application/json";

                    using (var writer = new BinaryWriter(response.OutputStream, response.ContentEncoding))
                    {
                        OnWriteResponse(httpListenerContext, compositeRootConfiguration, value, writer, response.ContentEncoding.GetBytes(responseContent), sessionToken);
                    }
                }

                wroteResponse = true;
            }
            catch (HttpListenerException)
            {
                wroteResponse = false;
            }

            OnAfterWriteResponse(httpListenerContext, compositeRootConfiguration, value, sessionToken);
            response.Close();

            return wroteResponse;
        }

        protected virtual void OnWriteResponse(HttpListenerContext httpListenerContext, CompositeRootConfiguration compositeRootConfiguration, object value, BinaryWriter writer, byte[] content, string sessionToken)
        {
            writer.Write(content);
        }

        protected virtual void OnAfterWriteResponse(HttpListenerContext httpListenerContext, CompositeRootConfiguration compositeRootConfiguration, object value, string sessionToken)
        {
        }

        internal void CompositeRoot_EventAdded(object sender, EventArgs e)
        {
            var compositeEvent = sender as CompositeEvent;
            
            if (compositeEvent.Mode == CompositeRootMode.SingleHost)
            {
                foreach (var compositeRootSession in ActiveSessions.Sessions.Values)
                    compositeRootSession.AddEvent(compositeEvent);
            }
            else if(compositeEvent.SessionToken != null && ActiveSessions.Sessions.ContainsKey(compositeEvent.SessionToken))
                ActiveSessions.Sessions[compositeEvent.SessionToken].AddEvent(compositeEvent);
        }

        protected virtual void OnWriteEvent(HttpListenerContext content, CompositeRoot compositeRoot, CompositeEvent compositeEvent, StreamWriter eventStreamWriter)
        {
        }

        private void WriteEvents(HttpListenerContext httpListenerContext, CompositeRootConfiguration compositeRootConfiguration, CompositeRoot compositeRoot)
        {
            if (httpListenerContext == null)
                throw new ArgumentNullException(nameof(httpListenerContext));

            if (compositeRoot == null)
                throw new ArgumentNullException(nameof(compositeRoot));

            var sessionToken = compositeRoot.CompositeRootSession.Token;
            var compositeRootSession = ActiveSessions.Sessions[sessionToken];

            if (compositeRootSession.IsEventStreamWriting)
                throw new InvalidOperationException();

            var response = httpListenerContext.Response;
            response.ContentEncoding = Encoding.UTF8;
            response.ContentType = "text/event-stream";
            response.KeepAlive = true;
            response.Headers.Add(HttpResponseHeader.CacheControl, "no-cache");
            response.Headers.Add(HttpResponseHeader.AcceptRanges, "bytes");

            compositeRootSession.AddEvent(new CompositeEvent(CompositeEventType.Listening, string.Empty, true, compositeRootConfiguration.Endpoint + sessionToken, compositeRootSession.Mode));

            using (var eventStreamWriter = new StreamWriter(response.OutputStream, response.ContentEncoding))
            {
                compositeRootSession.EventsDone.Reset();
                compositeRootSession.IsEventStreamWriting = true;

                var jsonSettings = new JsonSerializerSettings() { Formatting = Newtonsoft.Json.Formatting.None };
                jsonSettings.Converters.Add(new StringEnumConverter());


                while (true)
                {
                    CompositeEvent compositeEvent = null;
                    try
                    {
                        compositeEvent = compositeRootSession.TakeEvent();
                    }
                    catch (InvalidOperationException)
                    {
                        continue;
                    }

                    if (compositeEvent == null)
                        break;

                    var eventText = string.Format(CultureInfo.InvariantCulture, "data: {0}" + Environment.NewLine + Environment.NewLine, JsonConvert.SerializeObject(compositeEvent, jsonSettings));
                    eventStreamWriter.Write(eventText);
                    eventStreamWriter.Flush();

                    OnWriteEvent(httpListenerContext, compositeRoot, compositeEvent, eventStreamWriter);

                    if (compositeEvent.Data.ToString() == compositeRootConfiguration.Id.ToString())
                        break;
                }
            }

            response.Close();
            compositeRootSession.EventsDone.Set();
            compositeRootSession.IsEventStreamWriting = false;
        }

        private static IEnumerable<string> GetErrorMessages(Exception e)
        {
            return GetErrorMessages(e, new List<string>());
        }

        private static IEnumerable<string> GetErrorMessages(Exception e, List<string> messages)
        {
            if (e == null)
                return messages;

            messages.Add(e.Message);
            return GetErrorMessages(e.InnerException, messages);
        }

        private static void SetHttpListenerResponse(CompositeRootHttpContext context, HttpListenerResponse response)
        {
            if (context == null)
                return;

            response.ContentEncoding = context.Response.ContentEncoding ?? response.ContentEncoding;
            response.ContentLength64 = context.Response.ContentLength64 ?? response.ContentLength64;
            response.SendChunked = context.Response.SendChunked ?? response.SendChunked;
            response.StatusCode = context.Response.StatusCode ?? response.StatusCode;
            response.ContentType = !string.IsNullOrEmpty(context.Response.ContentType) ? context.Response.ContentType : response.ContentType;
            response.KeepAlive = context.Response.KeepAlive ?? response.KeepAlive;
            response.ProtocolVersion = context.Response.ProtocolVersion ?? response.ProtocolVersion;
            response.RedirectLocation = !string.IsNullOrEmpty(context.Response.RedirectLocation) ? context.Response.RedirectLocation : response.RedirectLocation;

            response.StatusDescription = !string.IsNullOrEmpty(context.Response.StatusDescription) ? context.Response.StatusDescription : response.StatusDescription;

            context.Response.GetCookies().ToList().ForEach(c => response.Cookies.Add(c));
            context.Response.GetHeaders().ToList().ForEach(h => response.Headers.Add(h.Key, h.Value));
        }

        public HttpListenerProperties HttpListenerProperties { get; private set; }

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
                _httpListener.Close();
                _backgroundTaskTimer.Dispose();
            }

            disposed = true;
        }

        ~CompositeRootHttpServer()
        {
            Dispose(false);
        }
    }
}