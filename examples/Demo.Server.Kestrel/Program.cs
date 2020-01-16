using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using XmlRpc.Kestrel;

namespace Demo.Server.Kestrel
{
    class Program
    {
        static void Main()
        {
            var host = new WebHostBuilder()
               .UseKestrel(o => o.Listen(IPAddress.Any, 5678))          // bind to any interface on given port
               .ConfigureServices(s => s.AddXmlRpc(new AddService()))   // register xml-rpc service class
               .Configure(c => c.UseXmlRpc<AddService>("/xmlrpc"))      // map route to service
               .Build();

            host.Run();
        }
    }
}
