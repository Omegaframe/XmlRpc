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
            var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5678/xmlrpc") };
            var config = new SerializerConfig();
            var proxy = XmlRpcClientBuilder.Create<IAddServiceProxy>(client, config);

            Console.WriteLine("Calling Demo.addNumbers with [3,4]...");
            var result = proxy.AddNumbers(3, 4);
            Console.WriteLine("Received result: " + result);

            Console.ReadKey();
        }
    }
}
