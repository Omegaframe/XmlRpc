using XmlRpc.Client;
using Demo.Contracts;

namespace XmlRpc.Core.ClientDemo
{
    public interface IAddServiceProxy : IXmlRpcProxy, IAddService
    {
    }
}
