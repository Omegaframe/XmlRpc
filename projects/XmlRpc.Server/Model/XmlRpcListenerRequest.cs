using System.IO;
using System.Net;
using XmlRpc.Server.Interfaces;

namespace XmlRpc.Server.Model
{
    public class XmlRpcListenerRequest : IHttpRequest
    {
        readonly HttpListenerRequest _request;

        public XmlRpcListenerRequest(HttpListenerRequest request)
        {
            _request = request;
        }

        public Stream InputStream => _request.InputStream;

        public string HttpMethod => _request.HttpMethod;
    }
}