using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using XmlRpc.Client.Attributes;
using XmlRpc.Client.Internals;
using XmlRpc.Client.Serializer.Model;
using XmlRpc.Client.Serializer.Request;
using XmlRpc.Client.Serializer.Response;

namespace XmlRpc.Client
{
    public class XmlRpcClient : IXmlRpcClient
    {
        public SerializerConfig Configuration { get; set; }

        readonly HttpClient _client;

        public XmlRpcClient(HttpClient client)
        {
            Configuration = new SerializerConfig();

            _client = client;
        }

        public object Invoke(MethodBase methodBase, params object[] parameters)
        {
            return Invoke(methodBase as MethodInfo, parameters);
        }

        public object Invoke(string methodName, params object[] parameters)
        {
            var methodInfo = XmlRpcClientProtocol.GetMethodInfoFromName(this, methodName, parameters);
            return Invoke(methodInfo, parameters);
        }

        public object Invoke(MethodInfo methodInfo, params object[] parameters)
        {
            var request = XmlRpcClientProtocol.MakeXmlRpcRequest(methodInfo, parameters);

            using var memoryStream = new MemoryStream();
            var serializer = new XmlRpcRequestSerializer(Configuration);
            serializer.SerializeRequest(memoryStream, request);

            memoryStream.Seek(0, SeekOrigin.Begin);
            using var requestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                Content = new StreamContent(memoryStream)
            };

            using var response = _client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            using var responseStream = response.Content.ReadAsStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var deserializer = new XmlRpcResponseDeserializer(Configuration);
            var responseAnswer = deserializer.DeserializeResponse(responseStream, request.MethodInfo.ReturnType);
            return responseAnswer.ReturnValue;
        }

        [XmlRpcMethod("system.listMethods")]
        public string[] SystemListMethods()
        {
            return (string[])Invoke("SystemListMethods");
        }

        [XmlRpcMethod("system.methodSignature")]
        public object[] SystemMethodSignature(string methodName)
        {
            return (object[])Invoke("SystemMethodSignature", new object[] { methodName });
        }

        [XmlRpcMethod("system.methodHelp")]
        public string SystemMethodHelp(string methodName)
        {
            return (string)Invoke("SystemMethodHelp", new object[] { methodName });
        }
    }
}
