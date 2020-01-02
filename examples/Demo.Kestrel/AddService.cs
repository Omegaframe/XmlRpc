using Demo.Contracts;

namespace XmlRpc.Kestrel
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
