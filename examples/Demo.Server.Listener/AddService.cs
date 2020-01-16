using Demo.Contracts;
using System;
using XmlRpc.Listener;

namespace XmlRpc.ServerDemo
{
    // define a xml-rpc service that implements your contracts
    // remember to extend XmlRpcService to ensure your service is recognized as XmlRpcService
    internal class AddService : XmlRpcService, IAddService
    {
        public int AddNumbers(int numberA, int numberB)
        {
            Console.WriteLine($"Received request to Demo.addNumbers. Parameters: [{numberA}, {numberB}]");
            return numberA + numberB;
        }
    }
}
