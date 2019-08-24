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
using CT.Hosting.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Linq;

namespace CT.Blog.Test.Systems.Actors.Administrator.Stories.Monitor_Application.Use_Cases.Review_Active_Users.Automated_Tests
{
    [TestClass]
    public class ReviewActiveUsersTest
    {
        private static CompositeRootHttpServerTester _tester;
        private static CompositeRootConfiguration _blogServerMonitorConfiguration;
        private static CompositeRootHttpServerTesterConnection _blogServerMonitorConnection;
        private static CompositeRootHttpServerConfiguration _blogServerHttpServerConfig;
        private static CompositeRootConfiguration _blogServerConfig;
        private static CompositeRootHttpServerTesterConnection _blogServerConnection;


        [ClassInitialize]
        public static void Setup(TestContext testContext)
        {
            _tester = new CompositeRootHttpServerTester(JsonConvert.DeserializeObject<RootHttpServerConfiguration>(File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "BlogServerMonitorConfig.json"))));
            _tester.Initialize();

            _blogServerMonitorConfiguration = _tester.Configuration.ServerRootConfigurations.RootConfigurations.Values.First();
            _blogServerHttpServerConfig = CompositeRootHttpServerConfiguration.Create(JsonConvert.DeserializeObject<RootHttpServerConfiguration>(File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "BlogServerConfig.json"))));
            _blogServerConfig = _blogServerHttpServerConfig.ServerRootConfigurations.RootConfigurations.Values.First();
        }

        [TestMethod]
        public void RetrieveActiveUsers()
        {
            _blogServerMonitorConnection = _tester.LogOnUser(_blogServerMonitorConfiguration, "username=admin&password=1234");
            _blogServerConnection = _tester.LogOnUser(_blogServerConfig, "username=admin&password=1234");

            var blogServerMonitorRoot = (JObject)CompositeRootHttpServerTester.SendRequest(_blogServerMonitorConfiguration, "BlogServer", _blogServerMonitorConnection.SessionToken).First().ReturnValue;

            Assert.IsTrue(blogServerMonitorRoot.SelectTokens("activeSessions.sessions[?(@..userName == 'admin')]").Count() == 1);
        }
    }
}
