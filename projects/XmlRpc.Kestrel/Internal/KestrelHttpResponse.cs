using Microsoft.AspNetCore.Http;
using System.IO;
using XmlRpc.Server.Interfaces;

namespace XmlRpc.Kestrel.Internal
{
    class KestrelHttpResponse : IHttpResponse
    {
        public string StatusDescription { get; set; }
        public long ContentLength
        {
            set => _kestrelResponse.ContentLength = value;
        }
        public string ContentType
        {
            get => _kestrelResponse.ContentType;
            set => _kestrelResponse.ContentType = value;
        }
        public int StatusCode
        {
            get => _kestrelResponse.StatusCode;
            set => _kestrelResponse.StatusCode = value;
        }
        public TextWriter Output => new StreamWriter(_kestrelResponse.Body);
        public Stream OutputStream => _kestrelResponse.Body;   

        readonly HttpResponse _kestrelResponse;

        public KestrelHttpResponse(HttpResponse kestrelResponse)
        {
            _kestrelResponse = kestrelResponse;
        }

        public void AddAdditionalHeaders(string key, string value)
        {
            _kestrelResponse.Headers.Add(key, value);
        }
    }
}
