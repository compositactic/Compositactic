using System;
using System.Runtime.Serialization;
using CT.Hosting;
using CT.Hosting.Configuration;

namespace CT.Server.Chat
{
    public class Program
    {
        static void Main(string[] args)
        {
            var serverUrl = "http://localhost:8002/Chat/";
            using (var server = CompositeRootHttpServerConfiguration.Create(serverUrl,
                                typeof(ChatRoot),
                                CompositeRootMode.SingleHost)
                                .CreateServer())
            {
                server.Start();
                Console.WriteLine("Navigate to: " + serverUrl);
                Console.ReadLine();
            }
        }
    }

    [DataContract]
    public class ChatRoot : CompositeRoot
    {
        public ChatRoot(CompositeRootConfiguration configuration) : base(configuration) { }

        [Command]
        public void SaySomething(CompositeRootHttpContext context, string message)
        {
            AddEvent(CompositeEventType.Custom, string.Empty, context.Request.UserName + " says: " + message);
        }
    }
}
