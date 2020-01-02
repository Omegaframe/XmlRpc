using Microsoft.AspNetCore.Http;
using System.IO;
using XmlRpc.Server.Interfaces;

namespace XmlRpc.Kestrel.Internal
{
    class KestrelHttpRequest : IHttpRequest
    {
        public Stream InputStream => _kestrelRequest.Body;
        public string HttpMethod => _kestrelRequest.Method;

        readonly HttpRequest _kestrelRequest;

        public KestrelHttpRequest(HttpRequest kestrelRequest)
        {
            _kestrelRequest = kestrelRequest;
        }
    }
}
