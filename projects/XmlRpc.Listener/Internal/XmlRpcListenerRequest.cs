using System.IO;
using System.Net;
using XmlRpc.Server.Interfaces;

namespace XmlRpc.Listener.Internal
{
    class XmlRpcListenerRequest : IHttpRequest
    {
        public Stream InputStream => _request.InputStream;
        public string HttpMethod => _request.HttpMethod;

        readonly HttpListenerRequest _request;

        public XmlRpcListenerRequest(HttpListenerRequest request)
        {
            _request = request;
        }
    }
}