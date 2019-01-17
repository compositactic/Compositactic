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

using System.Net;
using System.Security.Authentication.ExtendedProtection;
using static System.Net.HttpListener;

namespace CT.Hosting
{
    public class HttpListenerProperties
    {
        private HttpListener _httpListener;
        public HttpListenerProperties(HttpListener httpListener)
        {
            _httpListener = httpListener;
        }

        public AuthenticationSchemes AuthenticationSchemes
        {
            get { return _httpListener.AuthenticationSchemes; }
            set { _httpListener.AuthenticationSchemes = value; }
        }

        public AuthenticationSchemeSelector AuthenticationSchemeSelectorDelegate
        {
            get { return _httpListener.AuthenticationSchemeSelectorDelegate; }
            set { _httpListener.AuthenticationSchemeSelectorDelegate = value; }
        }

        public ServiceNameCollection DefaultServiceNames { get { return _httpListener.DefaultServiceNames; } }

        public ExtendedProtectionPolicy ExtendedProtectionPolicy
        {
            get { return _httpListener.ExtendedProtectionPolicy; }
            set { _httpListener.ExtendedProtectionPolicy = value; }
        }

        public ExtendedProtectionSelector ExtendedProtectionSelectorDelegate
        {
            get { return _httpListener.ExtendedProtectionSelectorDelegate; }
            set { _httpListener.ExtendedProtectionSelectorDelegate = value; }
        }

        public bool IgnoreWriteExceptions
        {
            get { return _httpListener.IgnoreWriteExceptions; }
            set { _httpListener.IgnoreWriteExceptions = value; }
        }

        public bool IsListening { get { return _httpListener.IsListening; } }

        public string Realm
        {
            get { return _httpListener.Realm; }
            set { _httpListener.Realm = value; }
        }

        public HttpListenerTimeoutManager TimeoutManager { get { return _httpListener.TimeoutManager; } }

        public bool UnsafeConnectionNtlmAuthentication
        {
            get { return _httpListener.UnsafeConnectionNtlmAuthentication; }
            set { _httpListener.UnsafeConnectionNtlmAuthentication = value; }
        }
    }
}
