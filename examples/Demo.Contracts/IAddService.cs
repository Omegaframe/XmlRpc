using XmlRpc.Client.Attributes;

namespace Demo.Contracts
{
    // interface defining the methods that should be available for the client to call and the server to execute.
    // it is required for both, server and client to reference to this interface
    public interface IAddService
    {
        // ensure to use the XmlRpcMethod Attribute on the Methods you want to make available on Xml-Rpc
        [XmlRpcMethod("Demo.addNumbers")]
        int AddNumbers(int numberA, int numberB);
    }
}
