using System;
using System.Linq;
using System.Reflection;
using XmlRpc.Client.Attributes;
using XmlRpc.Client.Exceptions;
using XmlRpc.Client.Model;

namespace XmlRpc.Client
{
    public class XmlRpcClientProtocol
    {
        public XmlRpcRequest MakeXmlRpcRequest(Guid id, MethodInfo methodInfo, object[] parameters, string xmlRpcMethod)
        {
            var rpcMethodName = GetRpcMethodName(methodInfo);
            return new XmlRpcRequest(rpcMethodName, parameters, methodInfo, xmlRpcMethod, id);
        }

        public MethodInfo GetMethodInfoFromName(object clientObj, string methodName, object[] parameters)
        {
            var paramTypes = Array.Empty<Type>();
            if (parameters != null)
            {
                if (parameters.Any(p => p == null))
                    throw new XmlRpcNullParameterException("Null parameters are invalid");

                paramTypes = new Type[parameters.Length];
                for (int i = 0; i < paramTypes.Length; i++)
                    paramTypes[i] = parameters[i].GetType();
            }

            var type = clientObj.GetType();
            var methodInfo = type.GetMethod(methodName, paramTypes);
            if (methodInfo != null)
                return methodInfo;

            try
            {
                methodInfo = type.GetMethod(methodName);
            }
            catch (AmbiguousMatchException)
            {
                throw new XmlRpcInvalidParametersException("Method parameters match the signature of more than one method");
            }

            if (methodInfo == null)
                throw new Exception("Invoke on non-existent or non-public proxy method");

            throw new XmlRpcInvalidParametersException("Method parameters do not match signature of any method called " + methodName);
        }

        string GetRpcMethodName(MethodInfo methodInfo)
        {
            var attr = Attribute.GetCustomAttribute(methodInfo, typeof(XmlRpcMethodAttribute));
            if (attr == null)
                throw new Exception("missing method attribute");

            var xrmAttr = attr as XmlRpcMethodAttribute;
            var rpcMethod = xrmAttr.Method;
            if (string.IsNullOrWhiteSpace(rpcMethod))
                rpcMethod = methodInfo.Name;

            return rpcMethod;
        }
    }
}


