using CT.Hosting.Configuration;
using Newtonsoft.Json;
using System;
using System.IO;


namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var configurationFile = Path.Combine(Environment.CurrentDirectory, "ShopMonitorConfig.json");
            var shopMonitorConfiguration = JsonConvert.DeserializeObject<RootHttpServerConfiguration>(File.ReadAllText(configurationFile));
            
            using (var server = CompositeRootHttpServerConfiguration.Create(shopMonitorConfiguration).CreateServer())
            {
                server.Start();
                Console.ReadLine();
            }
        }
    }
}



//http://stackoverflow.com/questions/11403333/httplistener-with-https-support
//http://stackoverflow.com/questions/499591/are-https-urls-encrypted