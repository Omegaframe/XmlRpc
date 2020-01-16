using System.Reflection;

namespace XmlRpc.Client.Model
{
    public class XmlRpcRequest
    {
        public string Method { get; set; }
        public object[] Arguments { get; set; }
        public MethodInfo MethodInfo { get; set; }

        public XmlRpcRequest(string methodName, object[] parameters, MethodInfo methodInfo)
        {
            Method = methodName;
            Arguments = parameters;
            MethodInfo = methodInfo;
        }

        public XmlRpcRequest(string methodName, object[] parameters)
        {
            Method = methodName;
            Arguments = parameters;
        }
    }
}