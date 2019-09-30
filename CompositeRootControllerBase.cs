using CT;
using CT.Hosting;
using CT.Hosting.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;

namespace Navient.NaviRefi.ServerASPNETWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class CompositeRootControllerBase : ControllerBase
    {
        protected RootHttpServerConfiguration RootConfiguration { get; }

        protected CompositeRootConfiguration CompositeRootConfiguration { get; }

        public CompositeRootControllerBase()
        {
            RootConfiguration = GetConfiguration();
            var config = CompositeRootHttpServerConfiguration.Create(RootConfiguration);
            CompositeRootConfiguration = config.ServerRootConfigurations.RootConfigurations.Values.First();
        }

        protected abstract RootHttpServerConfiguration GetConfiguration();

        protected virtual void SetCache(string sessionToken, string jsonValue)
        {
            var memoryCache = MemoryCache.Default;
            memoryCache.Set(sessionToken, jsonValue, DateTime.Now.Add(CompositeRootConfiguration.SessionExpiration));
        }

        protected virtual string GetCache(string sessionToken)
        {
            var memoryCache = MemoryCache.Default;
            return memoryCache.Get(sessionToken) as string;
        }

        [HttpGet]
        [HttpPost]
        public ActionResult<object> ReceiveRequest([FromForm] object _)
        {
            var requestBody = (string)null;

            var request = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
            IEnumerable<CompositeUploadedFile> uploadedFiles = null;
            IEnumerable<CompositeRootCommandRequest> commandRequests = null;

            var requestParts = request.Split('?');
            requestBody = requestParts.Length >= 3 ? requestParts[2] : null;

            if (requestBody == null && Request.ContentLength.HasValue)
                requestBody = Request.Body.GetRequest(Encoding.UTF8, Request.ContentType, string.Empty, CultureInfo.CurrentCulture, out uploadedFiles, out commandRequests);

            var compositeRootHttpContext = GetContext(requestBody, uploadedFiles);

            var compositeRoot = CompositeRoot.Create(CompositeRootConfiguration);
            var compositeRootModelFieldName = compositeRoot.GetType().GetCustomAttribute<CompositeModelAttribute>()?.ModelFieldName;
            var compositeRootModelField = compositeRoot.GetType().GetField(compositeRootModelFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            var compositeRootModelFieldType = compositeRootModelField.FieldType;

            var loginResponseJson = string.Empty;

            if (request.ToLowerInvariant().StartsWith("?" + nameof(CompositeRootAuthenticator.LogOn).ToLowerInvariant()))
            {
                var loginResponse = compositeRoot.Authenticator.Execute(nameof(CompositeRootAuthenticator.LogOn) + "?" + requestBody, null, compositeRootHttpContext, string.Empty, string.Empty, uploadedFiles).ReturnValue as CompositeRootAuthenticatorResponse;
                SetCache(loginResponse.SessionToken, JsonConvert.SerializeObject(compositeRootModelField.GetValue(compositeRoot)));
                Response.ContentType = "application/json";
                return loginResponse;
            }
            else
            {
                var sessionToken = requestParts[1].Split('/')[0];
                //var commandPath = Regex.Replace(request, @"^\?" + sessionToken + @"/?", string.Empty);

                var compositeRootModelJson = GetCache(sessionToken);
                compositeRootModelField.SetValue(compositeRoot, JsonConvert.DeserializeObject(compositeRootModelJson, compositeRootModelFieldType));

                var commandResponses = compositeRoot.Execute(commandRequests, compositeRootHttpContext, string.Empty, sessionToken, uploadedFiles);
                SetCache(sessionToken, JsonConvert.SerializeObject(compositeRootModelField.GetValue(compositeRoot)));

                if (commandResponses.First().ReturnValue is byte[] binaryResponse)
                {
                    Response.ContentType = compositeRootHttpContext.Response.ContentType;
                    Response.ContentLength = compositeRootHttpContext.Response.ContentLength64;
                    return commandResponses.First().ReturnValue;
                }
                else
                {
                    Response.ContentType = "application/json";
                    return (object)commandResponses;
                }
            }
        }

        private CompositeRootHttpContext GetContext(string requestBody, IEnumerable<CompositeUploadedFile> uploadedFiles)
        {
            var compositeRootHttpContext = new CompositeRootHttpContext
            (
                requestContentType: Request.ContentType,
                requestContentLength64: Request.ContentLength ?? 0,
                requestContentEncoding: Encoding.UTF8,
                httpMethod: Request.Method,
                queryString: Request.ContentLength.HasValue ?
                    new Dictionary<string, string>() :
                        !string.IsNullOrEmpty(requestBody) ?
                        new Dictionary<string, string>(requestBody.Split('&').Select(s => new KeyValuePair<string, string>(s.Split('=')[0], s.Split('=')[1]))) :
                        new Dictionary<string, string>(),
                requestCookies: Request.Cookies.Select(c => new Cookie(c.Key, c.Value)),
                requestHeaders: new Dictionary<string, string>(Request.Headers.Select(q => new KeyValuePair<string, string>(q.Key, q.Value))),
                acceptTypes: Request.Headers["Accept"],
                hasEntityBody: Request.ContentLength.HasValue,
                isAuthenticated: false,
                isLocal: false,
                isSecureConnection: Request.IsHttps,
                isWebSocketRequest: false,
                requestKeepAlive: true,
                localEndPoint: new IPEndPoint(HttpContext.Connection.LocalIpAddress, HttpContext.Connection.LocalPort),
                requestProtocolVersion: null, //new Version(Request.Protocol),
                remoteEndPoint: new IPEndPoint(HttpContext.Connection.RemoteIpAddress, HttpContext.Connection.RemotePort),
                requestTraceIdentifier: new Guid(),
                serviceName: string.Empty,
                url: new Uri(Request.Scheme + "://" + Request.Host + Request.Path + Request.QueryString), //
                urlReferrer: null, //
                userAgent: Request.Headers["User-Agent"],
                userHostAddress: string.Empty,
                userHostName: string.Empty,
                uploadedFiles: uploadedFiles,
                clientCertificate: HttpContext.Connection.ClientCertificate,
                clientCertificateError: 0,
                userLanguages: Request.Headers["Accept-Language"],
                userName: string.Empty,
                sessionToken: string.Empty
            );

            return compositeRootHttpContext;
        }
    }
}
