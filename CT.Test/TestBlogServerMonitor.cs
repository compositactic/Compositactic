using CT.Hosting;
using CT.Hosting.Configuration;
using CT.Hosting.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CT.Data.MicrosoftSqlServer;
using System.Linq;
using System.IO;
using System;

namespace CT.Blogs.Test
{
    public class TableInfo
    {
        public string Name { get; set; }
        public int ObjectId { get; set; }
    }

    [TestClass]
    public class TestBlogServerMonitor
    {
        private static CompositeRootHttpServerTester _blogServerMonitorTester;
        private static CompositeRootConfiguration _blogServerMonitorConfiguration;
        private static CompositeRootHttpServerTesterConnection _blogServerMonitorConnection;

        [ClassInitialize]
        public static void Setup(TestContext testContext)
        {
            //_blogServerMonitorTester = new CompositeRootHttpServerTester(JsonConvert.DeserializeObject<RootHttpServerConfiguration>(File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "BlogServerMonitorConfig.json"))));
            //_blogServerMonitorTester.Initialize();

            //_blogServerMonitorConfiguration = _blogServerMonitorTester.Configuration.ServerRootConfigurations.RootConfigurations.Values.First();
            //_blogServerMonitorConnection = _blogServerMonitorTester.LogOnUser(_blogServerMonitorConfiguration, "username=admin&password=1234");
        }


        [TestMethod]
        public void DirTest()
        {
            var dirs = Directory
                        .GetDirectories(Path.Combine(Environment.CurrentDirectory, "BlogApplications"), "", SearchOption.AllDirectories)
                        .GroupBy(d => new { Depth = d.Split(Path.DirectorySeparatorChar).Count(), Directory = d })
                        .OrderBy(g => g.Key.Depth).ThenBy(g => g.Key.Directory)
                        .Select(g => g.Key.Directory);
        }


        [TestMethod]
        public void MicrosoftSqlServerRepositoryTest()
        {
            var repository = MicrosoftSqlServerRepository.Create();

            using (var connection = repository.OpenConnection(@"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=SSPI;"))
            using (var transaction = repository.BeginTransaction(connection))
            {
                var objs = repository.Load<TableInfo>(connection,
                    transaction,
                    @"
                        WITH Query AS
                        (
	                        SELECT ROW_NUMBER() OVER(ORDER BY name DESC) AS RowNumber, name, object_id
	                        FROM sys.columns
                        )
                        SELECT RowNumber, name, object_id AS ObjectId
                        FROM Query
                        WHERE RowNumber BETWEEN 1 AND 10000

                    ", null);
            }
        }

        [TestMethod]
        public void MyTest()
        {
            var blogServerMonitorHelpResponse = CompositeRootHttpServerTester.SendRequest(_blogServerMonitorConfiguration, "?", _blogServerMonitorConnection.SessionToken);
            //var request = CompositeRootHttpServerTester.CreateRequest(_blogServerMonitorConfiguration, "~/test.txt", _blogServerMonitorConnection.SessionToken);
            #region
            //var fq = CompositeRootHttpServerTester.CreateRequest(_configuration, "~/test.txt", _connection.SessionToken);
            ////fq.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip");
            //fq.AddRange(-3);
            //fq.AddRange(6, 7);
            //fq.AddRange(-1);
            //fq.AddRange(0, 7);

            //fq.AddRange(100, 200);

            //var r1 = CompositeRootHostHttpServerTester.SendRequest(fq);

            //var tt = Execute("NullableInt");


            //var connection = _tester.LogOnUser(_configuration, "username=&password=");
            //connection.AddEventWaitHandle(@"Message$", new ManualResetEvent(false));

            //var tempFilePath1 = Path.GetTempFileName();
            //File.WriteAllText(tempFilePath1, "test1");

            //var tempFilePath2 = Path.GetTempFileName();
            //File.WriteAllText(tempFilePath2, "test2");

            //var e = CompositeRootHttpServerTester.SendRequest(_configuration, "?", connection.SessionToken);
            //var x = CompositeRootHttpServerTester.SendRequest(_configuration, "FrameContainer/?", connection.SessionToken);


            //var request = CompositeRootHttpServerTester.CreateRequest(_configuration, "Start?pa=123&pa=456&testLevel=Middle&testDateTime=06/13/1974&testTimeSpan=00:00:05&testInt=420&testBool=true&testString=&testDecimal=3.1415&testChar=%65", connection.SessionToken);

            //var blag = CompositeRootHttpServerTester.SendRequest(request, new FileInfo[] { new FileInfo(tempFilePath1), new FileInfo(tempFilePath2) });

            //connection.WaitForEvent(@"Message$");

            //var bwqwflkg = CompositeRootHttpServerTester.SendRequest(_configuration, "", connection.SessionToken);


            //var bwlkg = CompositeRootHttpServerTester.SendRequest(_configuration, "", connection.SessionToken);

            //var ergt = CompositeRootHttpServerTester.SendRequest(_configuration, "FrameContainer/Frames/0/ParticleContainer/Particles/0/Y?3.14", connection.SessionToken);


            int id = 1;
            var commands = new CompositeRootCommandRequest[]
            {
                CompositeRootCommandRequest.Create(id++, "ConfigurationSettings"),
                CompositeRootCommandRequest.Create(id++, "Start?pa=123&pa=456&testLevel=&testDateTime=06/13/1974&testTimeSpan=00:00:05&testInt=420&testBool=true&testString=%00&testDecimal=3.1415&testChar=%65"),
                CompositeRootCommandRequest.Create(id++, "NullableInt"),
                CompositeRootCommandRequest.Create(id++, "NullableInt?{1/ParticleCount}"),
                CompositeRootCommandRequest.Create(id++, "NullableInt"),
                CompositeRootCommandRequest.Create(id++, "Start?pa={1/ParticleCount}&pa=456&testLevel=&testDateTime=06/13/1974&testTimeSpan=00:00:05&testInt=420&testBool=true&testString=%00&testDecimal=3.1415&testChar=%65")
            };


            //request = CompositeRootHttpServerTester.CreateRequest(_configuration, connection.SessionToken);
            //var r2 = CompositeRootHttpServerTester.SendRequest(request, commands);


            //var responses = CompositeRootHttpServerTester.SendRequest(_configuration, commands, connection.SessionToken);

            //var z = CompositeRootHttpServerTester.CreateRequest(_configuration, "FrameContainer/Frames/[EAE5CF8C-81D4-4519-813E-D915925A75FE]", connection.SessionToken);
            //var tt = CompositeRootHttpServerTester.SendRequest(z);

            //var t = CompositeRootHttpServerTester.SendRequest(_configuration, "FrameContainer/Frames/99999", connection.SessionToken);

            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "FrameContainer/Frames/0/ParticleContainer/Particles/0", connection.SessionToken);

            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "FrameContainer/Frames/0/ParticleContainer/Particles/[Particle30619839]", connection.SessionToken);

            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "FrameContainer/Frames/[50125c6b-d26f-4415-8d34-82a34d37f1a9]", connection.SessionToken);

            //var r = CompositeRootHttpServerTester.SendRequest(_configuration, "CreateBlob", connection.SessionToken);


            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "?", connection.SessionToken);


            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "NullableInt", connection.SessionToken);

            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "NullableInt?25", connection.SessionToken);

            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "NullableInt", connection.SessionToken);
            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "NullableInt?", connection.SessionToken);
            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "NullableInt", connection.SessionToken);

            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "Level", connection.SessionToken);

            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "Level?Middle", connection.SessionToken);
            ////        public void Start(CompositeRootHostHttpContext context, SimulatorLevel testLevel, DateTime testDateTime, TimeSpan testTimeSpan, int testInt, bool testBool, string testString, decimal testDecimal, char testChar)
            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "Level", connection.SessionToken);


            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "Name?matt", connection.SessionToken);
            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "Name", connection.SessionToken);
            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "Name?", connection.SessionToken);


            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "Name", connection.SessionToken);

            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "Name?%00", connection.SessionToken);
            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "Name", connection.SessionToken);


            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "Configuration/FrameCount/?", connection.SessionToken);

            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "Configuration/FrameCount?25", connection.SessionToken);

            //t = CompositeRootHttpServerTester.SendRequest(_configuration, "Configuration/FrameCount", connection.SessionToken);
            #endregion
        }

        [ClassCleanup]
        public static void Shutdown()
        {
            if(_blogServerMonitorTester != null)
                _blogServerMonitorTester.Dispose();
        }

    }
}
