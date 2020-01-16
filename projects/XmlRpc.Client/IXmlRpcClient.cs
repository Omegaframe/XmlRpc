using XmlRpc.Client.Serializer.Model;

namespace XmlRpc.Client
{
    public interface IXmlRpcClient
    {        
        SerializerConfig Configuration { get; set; }

        string[] SystemListMethods();
        object[] SystemMethodSignature(string MethodName);
        string SystemMethodHelp(string MethodName);
    }
}
