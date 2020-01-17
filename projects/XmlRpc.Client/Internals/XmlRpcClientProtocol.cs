using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using XmlRpc.Client.Attributes;
using XmlRpc.Client.Exceptions;
using XmlRpc.Client.Model;

namespace XmlRpc.Client.Internals
{
    static class XmlRpcClientProtocol
    {
        public static MethodData[] GetXmlRpcMethods(Type serviceType)
        {
            var xmlRpcMethodInfos = new List<MethodData>();
            foreach (var methodInfo in SystemHelper.GetMethods(serviceType))
            {
                var xmlRpcName = GetXmlRpcMethodNameOrNull(methodInfo);
                if (string.IsNullOrWhiteSpace(xmlRpcName))
                    continue;

                var parameterInfos = methodInfo.GetParameters();
                var hasParamsParameter = parameterInfos.Any() ? Attribute.IsDefined(parameterInfos[^1], typeof(ParamArrayAttribute)) : false;
                var methodData = new MethodData(methodInfo, xmlRpcName, hasParamsParameter);

                xmlRpcMethodInfos.Add(methodData);
            }

            return xmlRpcMethodInfos.ToArray();
        }

        public static XmlRpcRequest MakeXmlRpcRequest(MethodInfo methodInfo, object[] parameters)
        {
            var rpcMethodName = GetXmlRpcMethodNameOrThrow(methodInfo);
            return new XmlRpcRequest(rpcMethodName, parameters, methodInfo);
        }

        public static MethodInfo GetMethodInfoFromName(object clientObj, string methodName, object[] parameters)
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

        static string GetXmlRpcMethodNameOrThrow(MethodInfo methodInfo)
        {
            var methodName = GetXmlRpcMethodNameOrNull(methodInfo);
            if (string.IsNullOrWhiteSpace(methodName))
                throw new Exception("missing method attribute");

            return methodName;
        }

        static string GetXmlRpcMethodNameOrNull(MethodInfo methodInfo)
        {
            var attribute = Attribute.GetCustomAttribute(methodInfo, typeof(XmlRpcMethodAttribute));
            if (attribute is XmlRpcMethodAttribute xmlAttribute)
                return string.IsNullOrWhiteSpace(xmlAttribute.Method) ? methodInfo.Name : xmlAttribute.Method;

            return null;
        }
    }
}


