using CT.Hosting.Configuration;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var shopMonitorConfiguration = JsonConvert.DeserializeObject<RootHttpServerConfiguration>(File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "ShopMonitorConfig.json")));
            var shopServerConfiguration = JsonConvert.DeserializeObject<RootHttpServerConfiguration>(File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "ShopServerConfig.json")));

            using (var server = CompositeRootHttpServerConfiguration.Create(shopMonitorConfiguration).CreateServer())
            {
                server.Start();
                OpenBrowser(shopServerConfiguration.RootConfigurations.First().Value.Endpoint);
                Console.ReadLine();
            }
        }

        static void OpenBrowser(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
    }
}