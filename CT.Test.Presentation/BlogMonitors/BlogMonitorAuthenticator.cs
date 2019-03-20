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

using CT.Hosting;
using CT.Blogs.Presentation.Properties;
using System;

namespace CT.Blogs.Presentation.BlogMonitors
{
    public class BlogMonitorAuthenticator : CompositeRootAuthenticator
    {
        public override CompositeRootAuthenticatorResponse LogOn(string userName, string password, CompositeRootHttpContext context)
        {
            if (userName?.ToLowerInvariant() == "admin" && password == "1234")
                return new CompositeRootAuthenticatorResponse(Guid.NewGuid().ToString(), true, string.Empty, userName);
            else
                return new CompositeRootAuthenticatorResponse(null, false, Resources.BlogMonitorAuthenticator_InvalidLogOn, null);
        }
    }
}