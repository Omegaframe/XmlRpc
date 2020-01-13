using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using XmlRpc.Client.Serializer.Model;
using XmlRpc.Client.Serializer.Request;

namespace XmlRpc.Client
{
    public class XmlRpcClient
    {
        public SerializerConfig Configuration { get; set; }
        public string XmlRpcMethod { get; set; }

        readonly HttpClient _client;
        readonly XmlRpcClientProtocol _protocol;

        public XmlRpcClient(Uri endpoint) : this(endpoint, TimeSpan.FromSeconds(180)) { }

        public XmlRpcClient(string endpoint) : this(new Uri(endpoint)) { }
        public XmlRpcClient(string endpoint, TimeSpan timeout) : this(new Uri(endpoint), timeout) { }

        public XmlRpcClient(Uri endpoint, TimeSpan timeout)
        {
            _client = new HttpClient { BaseAddress = endpoint, Timeout = timeout };
            _protocol = new XmlRpcClientProtocol();
        }
        public XmlRpcClient(HttpClient client)
        {
            _client = client;
            _protocol = new XmlRpcClientProtocol();
        }

        public object Invoke(MethodInfo methodInfo, params object[] parameters)
        {
            var req = _protocol.MakeXmlRpcRequest(methodInfo, parameters, XmlRpcMethod);

            var serializer = new XmlRpcRequestSerializer(Configuration);
            serializer.SerializeRequest(serStream, req);
        }
    }
}
