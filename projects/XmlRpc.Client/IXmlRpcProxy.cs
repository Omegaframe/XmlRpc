namespace XmlRpc.Client
{
    public interface IXmlRpcProxy
    {        
        string XmlRpcMethod { get; set; }

        string[] SystemListMethods();
        object[] SystemMethodSignature(string MethodName);
        string SystemMethodHelp(string MethodName);
    }
}
