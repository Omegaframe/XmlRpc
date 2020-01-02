using System;
using System.Collections;
using System.Linq;
using XmlRpc.Client.Attributes;
using XmlRpc.Client.Exceptions;
using XmlRpc.Client.Model;

namespace XmlRpc.Server.Protocol
{
    public class SystemMethodsBase : MarshalByRefObject
    {
        [XmlRpcMethod("system.multicall", IntrospectionMethod = true, Description = "Executes multiple requests in a single call.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "parameter is required for xmlrpc signature but is not used here")]
        public Array SystemMulticall(Array requests)
        {
            // do nothing. this is a placeholder. this is handeled in XmlRpcServerProtocol
            return Array.Empty<int>();
        }

        [XmlRpcMethod("system.listMethods", IntrospectionMethod = true, Description = "Return an array of all available XML-RPC methods on this Service.")]
        public string[] SystemListMethods()
        {
            var svcInfo = XmlRpcServiceInfo.CreateServiceInfo(GetType());
            var list = svcInfo.Methods.Where(m => !m.IsHidden).Select(m => m.XmlRpcName);

            return list.Distinct().ToArray();
        }

        [XmlRpcMethod("system.methodSignature", IntrospectionMethod = true, Description = "Given the name of a method, return an array of legal signatures. Each signature is an array of strings. The first item of each signature is the return type, and any others items are parameter types.")]
        public Array SystemMethodSignature(string methodName)
        {
            var mthdInfos = GetMethodInfosOrThrow(methodName);
            var overloads = new ArrayList();

            foreach (var mthdInfo in mthdInfos)
            {
                var returnType = XmlRpcServiceInfo.GetXmlRpcTypeString(mthdInfo.ReturnType);
                var parameters = mthdInfo.Parameters.Select(p => XmlRpcServiceInfo.GetXmlRpcTypeString(p.Type));

                var methodDescription = parameters.Prepend(returnType).ToArray();
                overloads.Add(methodDescription);
            }

            return overloads.ToArray();
        }

        [XmlRpcMethod("system.methodHelp", IntrospectionMethod = true, Description = "Given the name of a method, return a help string. Overloads are new line separated.")]
        public string SystemMethodHelp(string methodName)
        {
            var mthdInfos = GetMethodInfosOrThrow(methodName);
            var multiDes = string.Join(Environment.NewLine, mthdInfos.Select(m => m.Doc));
            return multiDes;
        }

        XmlRpcMethodInfo[] GetMethodInfosOrThrow(string methodName)
        {
            var svcInfo = XmlRpcServiceInfo.CreateServiceInfo(GetType());
            var mthdInfos = svcInfo.GetMethods(methodName);

            if (!mthdInfos.Any())
                throw new XmlRpcFaultException(880, $"Request for information on unsupported method '{methodName}'");

            if (mthdInfos.All(m => m.IsHidden))
                throw new XmlRpcFaultException(881, $"Request for information on hidden method '{methodName}'");

            var visibleMethods = mthdInfos.Where(m => !m.IsHidden);
            return visibleMethods.ToArray();
        }
    }
}
