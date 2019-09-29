using System;
using System.Reflection;

namespace XmlRpc.Client.Model
{
    public class XmlRpcRequest
    {
        static int _created;

        public string method = null;
        public object[] args = null;
        public MethodInfo mi = null;
        public Guid proxyId;
        public int number = System.Threading.Interlocked.Increment(ref _created);
        public string xmlRpcMethod = null;

        public XmlRpcRequest() { }

        public XmlRpcRequest(string methodName, object[] parameters, MethodInfo methodInfo)
        {
            method = methodName;
            args = parameters;
            mi = methodInfo;
        }

        public XmlRpcRequest(string methodName, object[] parameters, MethodInfo methodInfo, string XmlRpcMethod, Guid proxyGuid)
        {
            method = methodName;
            args = parameters;
            mi = methodInfo;
            xmlRpcMethod = XmlRpcMethod;
            proxyId = proxyGuid;
        }

        public XmlRpcRequest(string methodName, object[] parameters)
        {
            method = methodName;
            args = parameters;
        }
    }
}