using Microsoft.AspNetCore.Builder;
using XmlRpc.Kestrel.Internal;

namespace XmlRpc.Kestrel
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseXmlRpc<T>(this IApplicationBuilder builder, string route) where T : XmlRpcService
        {
            return builder.Map(route, b => b.UseMiddleware<XmlRpcMiddleware<T>>());
        }
    }
}
