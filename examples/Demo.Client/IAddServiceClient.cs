using XmlRpc.Client;
using Demo.Contracts;

namespace XmlRpc.Core.ClientDemo
{
    // building a client requires the IXmlRpcClient and the interface holding the contract to be extended.
    // provide this interface to the XmlRpcClientBuilder to get your actual client implementation.    
    public interface IAddServiceClient : IXmlRpcClient, IAddService
    {
    }
}
