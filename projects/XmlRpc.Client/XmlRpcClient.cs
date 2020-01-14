using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using XmlRpc.Client.Serializer.Model;
using XmlRpc.Client.Serializer.Request;
using XmlRpc.Client.Serializer.Response;

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

        public async Task<object> Invoke(CancellationToken cancellationToken, MethodInfo methodInfo, params object[] parameters)
        {
            var request = _protocol.MakeXmlRpcRequest(methodInfo, parameters, XmlRpcMethod);

            using var memoryStream = new MemoryStream();
            var serializer = new XmlRpcRequestSerializer(Configuration);
            serializer.SerializeRequest(memoryStream, request);

            memoryStream.Seek(0, SeekOrigin.Begin);
            using var requestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                Content = new StreamContent(memoryStream)
            };

            using var response = await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var deserializer = new XmlRpcResponseDeserializer(Configuration);
            return deserializer.DeserializeResponse(responseStream, request.mi.ReturnType);
        }
    }
}
