using System.IO;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using XmlRpc.Server.Interfaces;

namespace XmlRpc.AspNetCore.Adapter
{
    internal class HttpResponseAdapter : IHttpResponse
    {
        readonly HttpResponse _adaptee;

        public HttpResponseAdapter(HttpResponse adaptee)
        {
            _adaptee = adaptee;

            AdditionalHeaders = new WebHeaderCollection();
            foreach (var header in adaptee.Headers)
                foreach (var value in header.Value)
                    AdditionalHeaders.Add(header.Key, value);
        }

        public long ContentLength { set => _adaptee.ContentLength = value; }

        public string ContentType { get => _adaptee.ContentType; set => _adaptee.ContentType = value; }

        public TextWriter Output => new StreamWriter(_adaptee.Body);

        public Stream OutputStream => _adaptee.Body;

        public int StatusCode { get => _adaptee.StatusCode; set => _adaptee.StatusCode = value; }

        public string StatusDescription { get => ReasonPhrases.GetReasonPhrase(_adaptee.StatusCode); set => _ = value; }

        public WebHeaderCollection AdditionalHeaders { get; }
    }
}