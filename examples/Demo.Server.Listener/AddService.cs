using Demo.Contracts;
using XmlRpc.Listener;

namespace XmlRpc.ServerDemo
{
    internal class AddService : XmlRpcService, IAddService
    {
        public int AddNumbers(int numberA, int numberB)
        {
            System.Console.WriteLine($"Received request to Demo.addNumbers. Parameters: [{numberA}, {numberB}]");
            return numberA + numberB;
        }
    }
}
