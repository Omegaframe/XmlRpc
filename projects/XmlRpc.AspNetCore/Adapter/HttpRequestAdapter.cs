
using System.IO;
using Microsoft.AspNetCore.Http;
using XmlRpc.Server.Interfaces;

namespace XmlRpc.AspNetCore.Adapter
{
    internal class HttpRequestAdapter : IHttpRequest
    {
        readonly HttpRequest _adaptee;

        public HttpRequestAdapter(HttpRequest adaptee) 
        {
            _adaptee = adaptee;
        }
        public Stream InputStream => _adaptee.Body;

        public string HttpMethod => _adaptee.Method;
    }
}