using System;
using System.Net.Http;
using XmlRpc.Client;

namespace XmlRpc.Core.ClientDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:5678/xmlrpc") };
            var proxy = XmlRpcProxyGen.Create<IAddServiceProxy>(client);

            Console.WriteLine("Calling Demo.addNumbers with [3,4]...");
            var result = proxy.AddNumbers(3, 4);
            Console.WriteLine("Received result: " + result);
        }
    }
}
