using System;
using System.Linq;
using XmlRpc.Client.Attributes;
using XmlRpc.Client.Exceptions;
using XmlRpc.Client.Model;

namespace XmlRpc.Server.Protocol
{
    public class SystemMethodsBase : MarshalByRefObject
    {
        [XmlRpcMethod("system.listMethods", IntrospectionMethod = true, Description = "Return an array of all available XML-RPC methods on this Service.")]
        public string[] SystemListMethods()
        {
            var svcInfo = XmlRpcServiceInfo.CreateServiceInfo(GetType());
            var list = svcInfo.Methods.Where(m => !m.IsHidden).Select(m => m.XmlRpcName);

            return list.ToArray();
        }

        [XmlRpcMethod("system.methodSignature", IntrospectionMethod = true, Description = "Given the name of a method, return an array of legal signatures. Each signature is an array of strings. The first item of each signature is the return type, and any others items are parameter types.")]
        public Array SystemMethodSignature(string methodName)
        {
            //TODO: support overloaded methods

            var mthdInfo = GetMethodInfoOrThrow(methodName);

            var returnType = XmlRpcServiceInfo.GetXmlRpcTypeString(mthdInfo.ReturnType);
            var parameters = mthdInfo.Parameters.Select(p => XmlRpcServiceInfo.GetXmlRpcTypeString(p.Type));

            return parameters.Prepend(returnType).ToArray();
        }

        [XmlRpcMethod("system.methodHelp", IntrospectionMethod = true, Description = "Given the name of a method, return a help string.")]
        public string SystemMethodHelp(string methodName)
        {
            //TODO: support overloaded methods?

            var mthdInfo = GetMethodInfoOrThrow(methodName);
            return mthdInfo.Doc;
        }

        XmlRpcMethodInfo GetMethodInfoOrThrow(string methodName)
        {
            //TODO: support overloaded methods?

            var svcInfo = XmlRpcServiceInfo.CreateServiceInfo(GetType());
            var mthdInfo = svcInfo.GetMethod(methodName);

            if (mthdInfo == null)
                throw new XmlRpcFaultException(880, $"Request for information on unsupported method '{methodName}'");

            if (mthdInfo.IsHidden)
                throw new XmlRpcFaultException(881, $"Request for information on hidden method '{methodName}'");

            return mthdInfo;
        }
    }
}
