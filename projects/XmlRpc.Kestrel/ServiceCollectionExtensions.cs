using Microsoft.Extensions.DependencyInjection;
using XmlRpc.Kestrel.Internal;

namespace XmlRpc.Kestrel
{
    public static class ServiceCollectionExtensions
    {
        public static void AddXmlRpc<T>(this IServiceCollection services, T xmlrpcService) where T: XmlRpcService
        {
            services.AddSingleton(xmlrpcService);
            services.AddSingleton<XmlRpcMiddleware<T>>();
        }
    }
}
