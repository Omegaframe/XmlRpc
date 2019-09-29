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
            set { _response.ContentLength64 = value; }
        }

        string IHttpResponse.ContentType
        {
            get { return _response.ContentType; }
            set { _response.ContentType = value; }
        }

        TextWriter IHttpResponse.Output
        {
            get { return new StreamWriter(_response.OutputStream); }
        }

        Stream IHttpResponse.OutputStream
        {
            get { return _response.OutputStream; }
        }

        int IHttpResponse.StatusCode
        {
            get { return _response.StatusCode; }
            set { _response.StatusCode = value; }
        }

        string IHttpResponse.StatusDescription
        {
            get { return _response.StatusDescription; }
            set { _response.StatusDescription = value; }
        }
    }
}