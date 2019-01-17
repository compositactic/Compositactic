﻿// Compositactic - Made in the USA - Indianapolis, IN  - Copyright (c) 2017 Matt J. Crouch

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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CT.Hosting.Test
{
    public class CompositeRootHttpServerTesterConnection : IDisposable
    {
        internal CompositeRootHttpServerTesterConnection(string prefix, string sessionToken)
        {
            if (string.IsNullOrEmpty(sessionToken))
                throw new InvalidOperationException(Resources.MustHaveValidSessionToken);

            _eventWaitHandles = new Dictionary<string, EventWaitHandle>();
            _events = new StringBuilder();
            Client = new WebClient();
            Client.OpenReadCompleted += _webClient_OpenReadCompleted;
            AddEventWaitHandle("listening", new ManualResetEvent(false));

            Client.OpenReadAsync(new Uri(string.Format(CultureInfo.InvariantCulture, prefix + "{0}/event", sessionToken)));
            _eventWaitHandles["listening"].WaitOne();
            SessionToken = sessionToken;
        }

        public string SessionToken { get; }

        private Dictionary<string, EventWaitHandle> _eventWaitHandles;
        public IReadOnlyDictionary<string, EventWaitHandle> EventWaitHandles {  get { return _eventWaitHandles; } }
        public WebClient Client { get; }

        private StringBuilder _events;
        public string Events
        {
            get { return _events.ToString(); }
        }

        private void _webClient_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            using (var reader = new StreamReader(e.Result))
            {
                while (reader.BaseStream.CanRead && !reader.EndOfStream)
                {
                    var eventData = reader.ReadLine();
                    _events.AppendLine(eventData);
                    Match eventName;
                    if ((eventName = Regex.Match(eventData, @"^event: (?'eventName'.+)$")).Success)
                    {
                        var eventExpression = _eventWaitHandles.Keys.SingleOrDefault(ev => Regex.IsMatch(eventName.Groups["eventName"].Value, ev));
                        if(eventExpression != null)
                            _eventWaitHandles[eventExpression].Set();
                    }
                }
            }
        }

        public void AddEventWaitHandle(string eventExpression, EventWaitHandle eventWaitHandle)
        {
            _eventWaitHandles.Add(eventExpression, eventWaitHandle);
        }

        public void WaitForEvent(string eventExpression)
        {
            if (!_eventWaitHandles.ContainsKey(eventExpression))
                throw new KeyNotFoundException(eventExpression);

            _eventWaitHandles[eventExpression].WaitOne();
        }

        public void WaitForEvent(string eventExpression, int millisecondsTimeout)
        {
            if (!_eventWaitHandles.ContainsKey(eventExpression))
                throw new KeyNotFoundException(eventExpression);

            _eventWaitHandles[eventExpression].WaitOne(millisecondsTimeout);
        }

        public void WaitForEvent(string eventExpression, TimeSpan timeout)
        {
            if (!_eventWaitHandles.ContainsKey(eventExpression))
                throw new KeyNotFoundException(eventExpression);

            _eventWaitHandles[eventExpression].WaitOne(timeout);
        }

        public void WaitForEvent(string eventExpression, int millisecondsTimeout, bool exitContext)
        {
            if (!_eventWaitHandles.ContainsKey(eventExpression))
                throw new KeyNotFoundException(eventExpression);

            _eventWaitHandles[eventExpression].WaitOne(millisecondsTimeout, exitContext);
        }

        public void WaitForEvent(string eventExpression, TimeSpan timeout, bool exitContext)
        {
            if (!_eventWaitHandles.ContainsKey(eventExpression))
                throw new KeyNotFoundException(eventExpression);

            _eventWaitHandles[eventExpression].WaitOne(timeout, exitContext);
        }

        internal static byte[] SendRequest(WebRequest request, string contentType, Stream requestContentStream, out string responseContentType, out string responseContentEncoding)
        {
            return SendResponse(request, contentType, requestContentStream, out responseContentType, out responseContentEncoding);
        }

        internal static byte[] SendRequest(string url, string contentType, Stream requestContentStream, out string responseContentType, out string responseContentEncoding)
        {
            var request = WebRequest.Create(url);
            return SendResponse(request, contentType, requestContentStream, out responseContentType, out responseContentEncoding);
        }
        private static byte[] SendResponse(WebRequest request, string contentType, Stream requestContentStream, out string responseContentType, out string responseContentEncoding)
        {
            request.Method = "POST";
            request.ContentType = contentType;
            return GetResponseData(request, requestContentStream, out responseContentType, out responseContentEncoding);
        }

        internal static byte[] SendRequest(WebRequest request, out string responseContentType, out string responseContentEncoding)
        {
            return GetResponseData(request, null, out responseContentType, out responseContentEncoding);
        }

        private static byte[] GetResponseData(WebRequest request, Stream requestContentStream, out string responseContentType, out string responseContentEncoding)
        {
            using (var responseStream = new MemoryStream())
            {
                if (requestContentStream != null)
                {
                    using (var requestStream = request.GetRequestStream())
                    {
                        requestContentStream.Position = 0;
                        requestContentStream.CopyTo(requestStream);
                    }
                }

                using (var response = request.GetResponse())
                {
                    responseContentEncoding = response.Headers[HttpResponseHeader.ContentEncoding];
                    responseContentType = response.ContentType;
                    response.GetResponseStream().CopyTo(responseStream);
                    return responseStream.ToArray();
                }
            }
        }

        internal static byte[] SendMultipartFormDataRequest(string url, IEnumerable<string> files, IEnumerable<KeyValuePair<string, string>> formFields, out string responseContentType, out string responseContentEncoding)
        {
            var boundary = "----------------------------" + Guid.NewGuid().ToString();

            using (var requestContentStream = new MemoryStream())
            {
                PrepareRequestContentStream(files, formFields, boundary, requestContentStream);
                return SendRequest(url, "multipart/form-data; boundary=" + boundary, requestContentStream, out responseContentType, out responseContentEncoding);
            }
        }

        internal static byte[] SendMultipartFormDataRequest(WebRequest request, IEnumerable<string> files, IEnumerable<KeyValuePair<string, string>> formFields, out string responseContentType, out string responseContentEncoding)
        {
            var boundary = "----------------------------" + Guid.NewGuid().ToString();

            using (var requestContentStream = new MemoryStream())
            {
                PrepareRequestContentStream(files, formFields, boundary, requestContentStream);
                return SendRequest(request, "multipart/form-data; boundary=" + boundary, requestContentStream, out responseContentType, out responseContentEncoding);
            }
        }

        private static void PrepareRequestContentStream(IEnumerable<string> files, IEnumerable<KeyValuePair<string, string>> formFields, string boundary, MemoryStream requestContentStream)
        {
            var boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            var endBoundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--");

            var formdataTemplate = "--" + boundary + "\r\nContent-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}\r\n";
            if (formFields != null)
            {
                var formFieldData = Encoding.UTF8.GetBytes(string.Join(string.Empty, formFields.Select(kvp => string.Format(CultureInfo.InvariantCulture, formdataTemplate, kvp.Key, kvp.Value))));
                requestContentStream.Write(formFieldData, 0, formFieldData.Length);
            }

            foreach (var file in files)
            {
                requestContentStream.Write(boundarybytes, 0, boundarybytes.Length);

                var headerTemplate = "Content-Disposition: form-data; name=\"fname\"; filename=\"{0}\"\r\nContent-Type: {1}\r\n\r\n";

                var header = string.Format(CultureInfo.InvariantCulture, headerTemplate, file, ContentTypes.GetContentTypeFromFileExtension(Path.GetExtension(file)));
                var headerbytes = Encoding.UTF8.GetBytes(header);
                requestContentStream.Write(headerbytes, 0, headerbytes.Length);

                var fileBytes = File.ReadAllBytes(file);
                requestContentStream.Write(fileBytes, 0, fileBytes.Length);
            }

            requestContentStream.Write(endBoundaryBytes, 0, endBoundaryBytes.Length);
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
                foreach (var eventWaitHandle in _eventWaitHandles.Values)
                    eventWaitHandle.Dispose();

                if (Client != null)
                    Client.Dispose();
            }

            disposed = true;
        }

        ~CompositeRootHttpServerTesterConnection()
        {
            Dispose(false);
        }
    }
}
