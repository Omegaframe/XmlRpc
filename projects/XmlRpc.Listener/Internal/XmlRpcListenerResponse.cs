using System.IO;
using System.Net;
using XmlRpc.Server.Interfaces;

namespace XmlRpc.Listener.Internal
{
    class XmlRpcListenerResponse : IHttpResponse
    {
        string IHttpResponse.ContentType
        {
            get => _response.ContentType;
            set => _response.ContentType = value;
        }
        public int StatusCode
        {
            get => _response.StatusCode;
            set => _response.StatusCode = value;
        }
        public string StatusDescription
        {
            get => _response.StatusDescription;
            set => _response.StatusDescription = value;
        }
        public long ContentLength { set => _response.ContentLength64 = value; }
        public TextWriter Output => new StreamWriter(_response.OutputStream);
        public Stream OutputStream => _response.OutputStream;

        readonly HttpListenerResponse _response;

        public XmlRpcListenerResponse(HttpListenerResponse response)
        {
            _response = response;
            response.SendChunked = false;
        }

        public void AddAdditionalHeaders(string key, string value)
        {
            _response.Headers.Add(key, value);
        }
    }
}