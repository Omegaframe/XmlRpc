using System.IO;
using System.Net;
using XmlRpc.Server.Interfaces;

namespace XmlRpc.Server.Model
{
    public class XmlRpcListenerResponse : IHttpResponse
    {
        readonly HttpListenerResponse _response;

        public XmlRpcListenerResponse(HttpListenerResponse response)
        {
            _response = response;
            response.SendChunked = false;
        }

        long IHttpResponse.ContentLength
        {
            set => _response.ContentLength64 = value;
        }

        string IHttpResponse.ContentType
        {
            get => _response.ContentType;
            set => _response.ContentType = value;
        }

        TextWriter IHttpResponse.Output => new StreamWriter(_response.OutputStream);

        Stream IHttpResponse.OutputStream => _response.OutputStream;

        int IHttpResponse.StatusCode
        {
            get => _response.StatusCode;
            set => _response.StatusCode = value;
        }

        string IHttpResponse.StatusDescription
        {
            get => _response.StatusDescription;
            set => _response.StatusDescription = value;
        }

        public void AddAdditionalHeaders(string key, string value)
        {
            _response.Headers.Add(key, value);
        }
    }
}