using Microsoft.AspNetCore.Builder;
using System;
using XmlRpc.Kestrel.Internal;

namespace XmlRpc.Kestrel
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseXmlRpc<T>(this IApplicationBuilder builder, string route) where T : XmlRpcService
        {
            if (string.IsNullOrEmpty(route))
                throw new ArgumentException("xmlrpc route not set");

            if (route.Equals("/", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("xmlrpc is not allowed to use root path '/'");

            return builder.Map(route, b => b.UseMiddleware<XmlRpcMiddleware<T>>());
        }
    }
}
