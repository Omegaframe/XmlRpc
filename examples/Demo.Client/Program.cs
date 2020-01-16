using System;
using System.Net.Http;
using XmlRpc.Client;
using XmlRpc.Client.Serializer.Model;

namespace XmlRpc.Core.ClientDemo
{
    class Program
    {
        static void Main()
        {
            // build a httpclient that fits your needs (uri, credentials, certificates, etc..)
            var httpClient = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5678/xmlrpc") };

            // prepare a Serializer Configuration or use the default one
            var config = new SerializerConfig();

            // use the client builder to create an instance of your services' client. ensure your interface extends IXmlRpcClient and your contract interface
            var xmlRpcClient = XmlRpcClientBuilder.Create<IAddServiceClient>(httpClient, config);

            // call a method of the client and it will be executed on server side
            Console.WriteLine("Calling Demo.addNumbers with [3,4]...");
            var result = xmlRpcClient.AddNumbers(3, 4);
            Console.WriteLine("Received result: " + result);

            Console.ReadKey();
        }
    }
}
