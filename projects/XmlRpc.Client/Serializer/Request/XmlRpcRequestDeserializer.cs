using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using XmlRpc.Client.Attributes;
using XmlRpc.Client.Exceptions;
using XmlRpc.Client.Model;
using XmlRpc.Client.Serializer.Extensions;
using XmlRpc.Client.Serializer.Model;
using XmlRpc.Client.Serializer.Parser;

namespace XmlRpc.Client.Serializer.Request
{
    public class XmlRpcRequestDeserializer : XmlRpcSerializer
    {
        ParseStack _parseStack;

        public XmlRpcRequest DeserializeRequest(Stream inputStream, Type serviceType)
        {
            _parseStack = new ParseStack("request");

            var xdoc = XmlDocumentLoader.LoadXmlDocument(inputStream);
            return DeserializeRequest(xdoc, serviceType);
        }

        XmlRpcRequest DeserializeRequest(XmlDocument xdoc, Type serviceType)
        {
            var callNode = xdoc.SelectSingleNode("methodCall");
            var paramsNode = callNode.SelectSingleNode("params");
            var paramNodes = paramsNode.SelectChildNodes("param");

            var calledMethod = FindCalledMethod(xdoc);
            var methodInfo = FindMethodInfo(serviceType, calledMethod, paramNodes);
            var parameterInfos = methodInfo.GetParameters();

            var methodParameter = new List<object>();

            var normalParameter = DeserializeParameter(serviceType, paramNodes, parameterInfos);
            methodParameter.AddRange(normalParameter);

            var paramsParameter = DeserializeParamsParameter(paramNodes, parameterInfos);
            if (paramsParameter != null)
                methodParameter.Add(paramsParameter);

            return new XmlRpcRequest(calledMethod, methodParameter.ToArray(), methodInfo);
        }

        string FindCalledMethod(XmlDocument xdoc)
        {
            var callNode = xdoc.SelectSingleNode("methodCall");
            if (callNode == null)
                throw new XmlRpcInvalidXmlRpcException("Request XML not valid XML-RPC - missing methodCall element.");

            var methodNode = callNode.SelectSingleNode("methodName");
            if (methodNode?.FirstChild == null)
                throw new XmlRpcInvalidXmlRpcException("Request XML not valid XML-RPC - missing methodName element.");

            var methodName = methodNode.FirstChild.Value;
            if (string.IsNullOrWhiteSpace(methodName))
                throw new XmlRpcInvalidXmlRpcException("Request XML not valid XML-RPC - empty methodName.");

            return methodName;
        }

        MethodInfo FindMethodInfo(Type serviceType, string calledMethod, XmlNode[] paramNodes)
        {
            var possibleMethods = new MethodInfo[0];
            var parameterInfos = new ParameterInfo[0];

            var serviceInfo = XmlRpcServiceInfo.CreateServiceInfo(serviceType);
            possibleMethods = serviceInfo.GetMethodInfos(calledMethod);
            if (!possibleMethods.Any())
                throw new XmlRpcUnsupportedMethodException($"unsupported method called: {calledMethod}");

            var methodInfo = possibleMethods.FirstOrDefault(m => m.GetParameters().Length == paramNodes.Length);
            if (methodInfo == null)
                throw new XmlRpcInvalidParametersException($"The method {methodInfo} was called with wrong parameter count");

            var rpcAttribute = Attribute.GetCustomAttribute(methodInfo, typeof(XmlRpcMethodAttribute));
            if (rpcAttribute == null)
                throw new XmlRpcMethodAttributeException($"Method {calledMethod} must be marked with the XmlRpcMethod attribute.");

            return methodInfo;
        }

        object[] DeserializeParameter(Type serviceType, XmlNode[] paramNodes, ParameterInfo[] parameterInfos)
        {
            var parser = new XmlParser(Configuration);
            var parameterObjects = new List<object>();
            var paramterCount = GetParamsPos(parameterInfos) ?? parameterInfos.Length;

            for (int i = 0; i < paramterCount; i++)
            {
                var paramNode = paramNodes[i];
                var valueNode = paramNode.SelectSingleNode("value");
                if (valueNode == null)
                    throw new XmlRpcInvalidXmlRpcException("Missing value element.");

                var node = valueNode.SelectValueNode();
                if (serviceType != null)
                {
                    _parseStack.Push($"parameter {i + 1}");
                    var parsedValue = parser.ParseValue(node, parameterInfos[i].ParameterType, _parseStack);
                    parameterObjects.Add(parsedValue);
                }
                else
                {
                    _parseStack.Push($"parameter {i}");
                    var parsedValue = parser.ParseValue(node, null, _parseStack);
                    parameterObjects.Add(parsedValue);
                }

                _parseStack.Pop();
            }

            return parameterObjects.ToArray();
        }

        Array DeserializeParamsParameter(XmlNode[] paramNodes, ParameterInfo[] parameterInfos)
        {
            var paramsPosition = GetParamsPos(parameterInfos);
            if (!paramsPosition.HasValue)
                return null;

            var parser = new XmlParser(Configuration);
            var paramsParameter = parameterInfos[paramsPosition.Value];
            var paramsType = paramsParameter.ParameterType.GetElementType();
            var numberOfParamsParameter = paramNodes.Length - paramsPosition;
            var paramsArray = (Array)Activator.CreateInstance(paramsParameter.ParameterType, numberOfParamsParameter);

            for (int i = 0; i < paramsArray.Length; i++)
            {
                var paramNode = paramNodes[i + paramsPosition.Value];
                var valueNode = paramNode.SelectSingleNode("value");
                if (valueNode == null)
                    throw new XmlRpcInvalidXmlRpcException("Missing value element.");

                var node = valueNode.SelectValueNode();
                _parseStack.Push($"parameter {i + 1 + paramsPosition}");
                paramsArray.SetValue(parser.ParseValue(node, paramsType, _parseStack), i);
                _parseStack.Pop();
            }

            return paramsArray;
        }

        int? GetParamsPos(ParameterInfo[] parameterInfo)
        {
            if (parameterInfo.Length == 0)
                return null;

            if (Attribute.IsDefined(parameterInfo[^1], typeof(ParamArrayAttribute)))
                return parameterInfo.Length - 1;

            return null;
        }
    }
}
