using System;
using System.IO;
using System.Xml;
using XmlRpc.Client.Exceptions;
using XmlRpc.Client.Model;

namespace XmlRpc.Client.Serializer
{
    public class XmlRpcResponseDeserializer : XmlRpcSerializer
    {
        public XmlRpcResponse DeserializeResponse(Stream stm, Type svcType)
        {
            if (Configuration.AllowInvalidHTTPContent())
            {
                stm = CopyStream(stm);
                RemoveLineBreaks(stm); // why are we doing this?
            }

            var xdoc = XmlDocumentLoader.LoadXmlDocument(stm);
            return DeserializeResponse(xdoc, svcType);
        }

        public XmlRpcResponse DeserializeResponse(XmlDocument xdoc, Type returnType)
        {
            var parser = new XmlParser(Configuration);
            var response = new XmlRpcResponse();
            var methodResponseNode = xdoc.SelectSingleNode("methodResponse");
            if (methodResponseNode == null)
                throw new XmlRpcInvalidXmlRpcException("Response XML not valid XML-RPC - missing methodResponse element.");

            var faultNode = methodResponseNode.SelectSingleNode("fault");
            if (faultNode != null)
            {
                var parseStack = new ParseStack("fault response");
                var faultEx = parser.ParseFault(faultNode, parseStack);
                throw faultEx;
            }

            var paramsNode = methodResponseNode.SelectSingleNode("params");
            if (paramsNode == null && returnType != null)
            {
                if (returnType == typeof(void))
                    return new XmlRpcResponse(null);
                else
                    throw new XmlRpcInvalidXmlRpcException("Response XML not valid XML-RPC - missing params element.");
            }

            var paramNode = paramsNode.SelectSingleNode("param");
            if (paramNode == null && returnType != null)
            {
                if (returnType == typeof(void))
                    return new XmlRpcResponse(null);
                else
                    throw new XmlRpcInvalidXmlRpcException("Response XML not valid XML-RPC - missing params element.");
            }

            var valueNode = paramNode.SelectSingleNode("value");
            if (valueNode == null)
                throw new XmlRpcInvalidXmlRpcException("Response XML not valid XML-RPC - missing value element.");

            response.retVal = null;
            if (returnType != typeof(void))
            {
                var parseStack = new ParseStack("response");
                var node = valueNode.SelectValueNode();
                response.retVal = parser.ParseValue(node, returnType, parseStack);
            }

            return response;
        }

        MemoryStream CopyStream(Stream inputStream)
        {
            var newStm = new MemoryStream();
            inputStream.CopyTo(newStm);
            newStm.Seek(0, SeekOrigin.Begin);

            return newStm;
        }

        void RemoveLineBreaks(Stream stm)
        {
            while (true)
            {
                var byt = stm.ReadByte();
                if (byt == -1)
                    throw new XmlRpcIllFormedXmlException("Response from server does not contain valid XML.");

                if (byt != 0x0d && byt != 0x0a && byt != ' ' && byt != '\t')
                {
                    stm.Position -= 1;
                    break;
                }
            }
        }
    }
}
