using Microsoft.AspNetCore.Http;
using System.IO;
using XmlRpc.Server.Interfaces;

namespace XmlRpc.Kestrel.Internal
{
    class KestrelHttpRequest : IHttpRequest
    {
        public Stream InputStream => _kestrelContext.Request.Body;
        public string HttpMethod => _kestrelContext.Request.Method;

        readonly HttpContext _kestrelContext;

        public KestrelHttpRequest(HttpContext kestrelContext)
        {
            _kestrelContext = kestrelContext;
        }
    }
}
