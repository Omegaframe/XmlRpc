using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using XmlRpc.Kestrel;

namespace Demo.Kestrel
{
    class Program
    {
        static void Main()
        {
            var host = new WebHostBuilder()
               .UseKestrel(o =>
               {
                   o.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30);
                   o.Limits.MaxRequestBodySize = null;
                   o.Limits.MaxRequestBufferSize = null;
                   o.Limits.MaxResponseBufferSize = null;
                   o.Listen(IPAddress.Any, 5678);
               })
               .UseStartup<StartupConfig>()
               .Build();

            host.Run();
        }
    }

    class StartupConfig
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddXmlRpc(new AddService());
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseXmlRpc<AddService>("/xmlrpc");
        }
    }
}
