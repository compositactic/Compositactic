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

using CT.Hosting;
using CT.Hosting.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System;
using CT.Blogs.Presentation.BlogMonitors;

namespace CT.Blogs.Presentation.BlogApplications
{
    [DataContract]
    [ParentProperty(nameof(BlogApplicationServer.BlogMonitor), nameof(BlogServerMonitorCompositeRoot.BlogServer))]
    public class BlogApplicationServer : CompositeRootHttpServer
    {
        public BlogApplicationServer(CompositeRootHttpServerConfiguration configuration) : base(configuration)
        {
        }

        public BlogApplicationServer(CompositeRootHttpServerConfiguration configuration, IEnumerable<IService> services) : base(configuration, services)
        {
        }

        public BlogApplicationServer(CompositeRootHttpServerConfiguration configuration, IEnumerable<Assembly> serviceAssemblies) : base(configuration, serviceAssemblies)
        {
        }

        public BlogServerMonitorCompositeRoot BlogMonitor { get; internal set; }

        protected override void OnAfterConfiguration(CompositeRootConfiguration configuration)
        {
            Console.WriteLine(configuration.Endpoint + " - " + configuration.EndpointPublicDirectory);
            base.OnAfterConfiguration(configuration);
        }

        protected override void OnAfterExecuteCommandRequests(HttpListenerContext httpListenerContext, CompositeRootConfiguration compositeRootConfiguration, object returnValue, IEnumerable<CompositeRootCommandResponse> commandResponses, string sessionToken)
        {
            base.OnAfterExecuteCommandRequests(httpListenerContext, compositeRootConfiguration, returnValue, commandResponses, sessionToken);
        }

        protected override void OnAfterLogOn(HttpListenerContext httpListenerContext, CompositeRootConfiguration compositeRootConfiguration, string requestParameters, IEnumerable<CompositeUploadedFile> uploadedFiles, CompositeRootAuthenticatorResponse authenticatorResponse)
        {
            base.OnAfterLogOn(httpListenerContext, compositeRootConfiguration, requestParameters, uploadedFiles, authenticatorResponse);
        }

        protected override void OnAfterWriteFile(HttpListenerContext httpListenerContext, FileInfo fileInfo, CompositeRoot compositeRoot, string sessionToken)
        {
            base.OnAfterWriteFile(httpListenerContext, fileInfo, compositeRoot, sessionToken);
        }

        protected override void OnAfterWriteResponse(HttpListenerContext httpListenerContext, CompositeRootConfiguration compositeRootConfiguration, object value, string sessionToken)
        {
            base.OnAfterWriteResponse(httpListenerContext, compositeRootConfiguration, value, sessionToken);
        }

        protected override RequestProcessingAction OnBeforeExecuteCommandRequests(HttpListenerContext httpListenerContext, CompositeRoot compositeRoot, string sessionToken, IEnumerable<CompositeRootCommandRequest> commandRequests, IEnumerable<CompositeUploadedFile> uploadedFiles)
        {
            return base.OnBeforeExecuteCommandRequests(httpListenerContext, compositeRoot, sessionToken, commandRequests, uploadedFiles);
        }

        protected override RequestProcessingAction OnBeforeLogOn(HttpListenerContext httpListenerContext, CompositeRootConfiguration compositeRootConfiguration, string requestParameters, IEnumerable<CompositeUploadedFile> uploadedFiles)
        {
            return base.OnBeforeLogOn(httpListenerContext, compositeRootConfiguration, requestParameters, uploadedFiles);
        }

        protected override RequestProcessingAction OnBeforeWriteFile(HttpListenerContext httpListenerContext, FileInfo fileInfo, CompositeRoot compositeRoot, string sessionToken)
        {
            return base.OnBeforeWriteFile(httpListenerContext, fileInfo, compositeRoot, sessionToken);
        }

        protected override RequestProcessingAction OnBeforeWriteResponse(HttpListenerContext httpListenerContext, CompositeRootConfiguration compositeRootConfiguration, object value, string sessionToken)
        {
            return base.OnBeforeWriteResponse(httpListenerContext, compositeRootConfiguration, value, sessionToken);
        }

        protected override void OnConfigurationError(CompositeRootConfiguration configuration, string errorMessage)
        {
            base.OnConfigurationError(configuration, errorMessage);
        }

        protected override string OnGetSessionTokenFromRequest(HttpListenerContext httpListenerContext, CompositeRootConfiguration compositeRootConfiguration)
        {
            return base.OnGetSessionTokenFromRequest(httpListenerContext, compositeRootConfiguration);
        }

        protected override void OnWriteEvent(HttpListenerContext content, CompositeRoot compositeRoot, CompositeEvent compositeEvent, StreamWriter eventStreamWriter)
        {
            base.OnWriteEvent(content, compositeRoot, compositeEvent, eventStreamWriter);
        }

        protected override void OnWriteFile(HttpListenerContext httpListenerContext, CompositeRoot compositeRoot, FileInfo requestedFileInfo, Stream outputStream, string sessionToken, long fileOffsetFromBeginning, long fileReadLength, bool isPartialContent, bool isPartialContentSingleRange, string partialContentBoundary, bool isLastRequestedRange)
        {
            base.OnWriteFile(httpListenerContext, compositeRoot, requestedFileInfo, outputStream, sessionToken, fileOffsetFromBeginning, fileReadLength, isPartialContent, isPartialContentSingleRange, partialContentBoundary, isLastRequestedRange);
        }

        protected override void OnWriteHomeFile(HttpListenerContext httpListenerContext, CompositeRootConfiguration compositeRootConfiguration, Stream responseStream, byte[] content)
        {
            base.OnWriteHomeFile(httpListenerContext, compositeRootConfiguration, responseStream, content);
        }

        protected override void OnWriteResponse(HttpListenerContext httpListenerContext, CompositeRootConfiguration compositeRootConfiguration, object value, BinaryWriter writer, byte[] content, string sessionToken)
        {
            base.OnWriteResponse(httpListenerContext, compositeRootConfiguration, value, writer, content, sessionToken);
        }

        protected override void OnBackgroundTask()
        {
            base.OnBackgroundTask();
        }
    }
}
