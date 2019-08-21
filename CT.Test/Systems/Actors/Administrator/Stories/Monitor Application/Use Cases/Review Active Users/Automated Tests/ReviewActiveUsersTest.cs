using CT.Hosting;
using CT.Hosting.Configuration;
using CT.Hosting.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using System;
using System.Threading;

namespace CT.Blog.Test.Systems.Actors.Administrator.Stories.Monitor_Application.Use_Cases.Review_Active_Users.Automated_Tests
{
    [TestClass]
    public class ReviewActiveUsersTest
    {
        private static CompositeRootHttpServerTester _blogServerMonitorTester;
        private static CompositeRootConfiguration _blogServerMonitorConfiguration;
        private static CompositeRootHttpServerTesterConnection _blogServerMonitorConnection;

        [ClassInitialize]
        public static void Setup(TestContext testContext)
        {
            _blogServerMonitorTester = new CompositeRootHttpServerTester(JsonConvert.DeserializeObject<RootHttpServerConfiguration>(File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "BlogServerMonitorConfig.json"))));
            _blogServerMonitorTester.Initialize();

            _blogServerMonitorConfiguration = _blogServerMonitorTester.Configuration.ServerRootConfigurations.RootConfigurations.Values.First();
            _blogServerMonitorConnection = _blogServerMonitorTester.LogOnUser(_blogServerMonitorConfiguration, "username=admin&password=1234");

        }


        [TestMethod]
        public void RetrieveActiveUsers()
        {

        }
    }
}
