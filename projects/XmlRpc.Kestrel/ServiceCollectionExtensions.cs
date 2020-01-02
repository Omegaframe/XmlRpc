using Microsoft.Extensions.DependencyInjection;
using XmlRpc.Kestrel.Internal;

namespace XmlRpc.Kestrel
{
    public static class ServiceCollectionExtensions
    {
        public static void AddXmlRpc<T>(this IServiceCollection services) where T: XmlRpcService
        {
            services.AddSingleton<T>();
            services.AddSingleton<XmlRpcMiddleware<T>>();
        }
    }
}
