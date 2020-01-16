namespace XmlRpc.Client.Model
{
    public class XmlRpcResponse
    {
        public object ReturnValue { get; set; }

        public XmlRpcResponse(object returnValue)
        {
            ReturnValue = returnValue;
        }        
    }
}