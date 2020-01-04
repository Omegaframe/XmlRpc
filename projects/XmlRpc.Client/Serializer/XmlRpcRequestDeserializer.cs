using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using XmlRpc.Client.Attributes;
using XmlRpc.Client.Exceptions;
using XmlRpc.Client.Model;

namespace XmlRpc.Client.Serializer
{
    public class XmlRpcRequestDeserializer : XmlRpcSerializer
    {
        public XmlRpcRequest DeserializeRequest(Stream stm, Type svcType)
        {
            if (stm == null)
                throw new ArgumentNullException(nameof(stm), "XmlRpcRequestDeserializer.DeserializeRequest");

            var xdoc = XmlDocumentLoader.LoadXmlDocument(stm);
            return DeserializeRequest(xdoc, svcType);
        }

        XmlRpcRequest DeserializeRequest(XmlDocument xdoc, Type svcType)
        {
            var parser = new XmlParser(Configuration);
            var request = new XmlRpcRequest();

            var callNode = xdoc.SelectSingleNode("methodCall");
            if (callNode == null)
                throw new XmlRpcInvalidXmlRpcException("Request XML not valid XML-RPC - missing methodCall element.");

            var methodNode = callNode.SelectSingleNode("methodName");
            if (methodNode?.FirstChild == null)
                throw new XmlRpcInvalidXmlRpcException("Request XML not valid XML-RPC - missing methodName element.");

            request.method = methodNode.FirstChild.Value;
            if (request.method == "")
                throw new XmlRpcInvalidXmlRpcException("Request XML not valid XML-RPC - empty methodName.");

            request.mi = null;
            var possibleMethods = new MethodInfo[0];
            var pis = new ParameterInfo[0];

            var svcInfo = XmlRpcServiceInfo.CreateServiceInfo(svcType);
            possibleMethods = svcInfo.GetMethodInfos(request.method);
            if (!possibleMethods.Any())
                throw new XmlRpcUnsupportedMethodException($"unsupported method called: {request.method}");

            // todo: overloads with parameter types instead of simple count
            // get overloaded method if any
            var paramsNode = callNode.SelectSingleNode("params");
            var paramNodes = paramsNode.SelectChildNodes("param");
            request.mi = possibleMethods.FirstOrDefault(m => m.GetParameters().Length == paramNodes.Length);
            if (request.mi == null)
                throw new XmlRpcInvalidParametersException($"The method {request.method} was called with wrong parameter count");

            var attr = Attribute.GetCustomAttribute(request.mi, typeof(XmlRpcMethodAttribute));
            if (attr == null)
                throw new XmlRpcMethodAttributeException($"Method {request.method} must be marked with the XmlRpcMethod attribute.");

            pis = request.mi.GetParameters();
            var paramsPos = GetParamsPos(pis);

            var parseStack = new ParseStack("request");
            var paramObjCount = (paramsPos == -1 ? paramNodes.Length : paramsPos + 1);
            var ordinaryParams = (paramsPos == -1 ? paramNodes.Length : paramsPos);
            var paramObjs = new object[paramObjCount];

            for (int i = 0; i < ordinaryParams; i++)
            {
                var paramNode = paramNodes[i];
                var valueNode = paramNode.SelectSingleNode("value");
                if (valueNode == null)
                    throw new XmlRpcInvalidXmlRpcException("Missing value element.");

                var node = valueNode.SelectValueNode();
                if (svcType != null)
                {
                    parseStack.Push($"parameter {i + 1}");
                    paramObjs[i] = parser.ParseValue(node, pis[i].ParameterType, parseStack);
                }
                else
                {
                    parseStack.Push($"parameter {i}");
                    paramObjs[i] = parser.ParseValue(node, null, parseStack);
                }
                parseStack.Pop();
            }

            if (paramsPos != -1)
            {
                var paramsType = pis[paramsPos].ParameterType.GetElementType();
                var args = new object[1];
                args[0] = paramNodes.Length - paramsPos;
                var varargs = (Array)Activator.CreateInstance(pis[paramsPos].ParameterType, args);

                for (int i = 0; i < varargs.Length; i++)
                {
                    var paramNode = paramNodes[i + paramsPos];
                    var valueNode = paramNode.SelectSingleNode("value");
                    if (valueNode == null)
                        throw new XmlRpcInvalidXmlRpcException("Missing value element.");

                    var node = valueNode.SelectValueNode();
                    parseStack.Push($"parameter {i + 1 + paramsPos}");
                    varargs.SetValue(parser.ParseValue(node, paramsType, parseStack), i);
                    parseStack.Pop();
                }
                paramObjs[paramsPos] = varargs;
            }
            request.args = paramObjs;
            return request;
        }

        int GetParamsPos(ParameterInfo[] pis)
        {
            if (pis.Length == 0)
                return -1;

            if (Attribute.IsDefined(pis[^1], typeof(ParamArrayAttribute)))
                return pis.Length - 1;

            return -1;
        }
    }
}
