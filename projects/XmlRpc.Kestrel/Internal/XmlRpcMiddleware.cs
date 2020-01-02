using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace XmlRpc.Kestrel.Internal
{
    class XmlRpcMiddleware<T> : IMiddleware where T : XmlRpcService
    {
        readonly T _xmlrpcService;

        public XmlRpcMiddleware(T xmlrpcService)
        {
            _xmlrpcService = xmlrpcService;
        }

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var request = new KestrelHttpRequest(context.Request);
            var response = new KestrelHttpResponse(context.Response);

            _xmlrpcService.HandleHttpRequest(request, response);

            // todo: make async
            return Task.CompletedTask;
        }
    }
}
