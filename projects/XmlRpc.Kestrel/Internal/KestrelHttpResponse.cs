using Microsoft.AspNetCore.Http;
using System.IO;
using XmlRpc.Server.Interfaces;

namespace XmlRpc.Kestrel.Internal
{
    class KestrelHttpResponse : IHttpResponse
    {
        public long ContentLength
        {
            set => _kestrelContext.Response.ContentLength = value;
        }

        public string ContentType
        {
            get => _kestrelContext.Response.ContentType;
            set => _kestrelContext.Response.ContentType = value;
        }

        public TextWriter Output => new StreamWriter(_kestrelContext.Response.Body);

        public Stream OutputStream => _kestrelContext.Response.Body;

        public int StatusCode { get => _kestrelContext.Response.StatusCode; set => _kestrelContext.Response.StatusCode = value; }
        public string StatusDescription { get; set; }

        readonly HttpContext _kestrelContext;

        public KestrelHttpResponse(HttpContext kestrelContext)
        {
            _kestrelContext = kestrelContext;
        }

        public void AddAdditionalHeaders(string key, string value)
        {
            _kestrelContext.Response.Headers.Add(key, value);
        }
    }
}
