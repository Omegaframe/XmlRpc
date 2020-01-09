using System;
using System.IO;
using System.Xml;
using XmlRpc.Client.Exceptions;
using XmlRpc.Client.Model;
using XmlRpc.Client.Serializer.Extensions;
using XmlRpc.Client.Serializer.Model;
using XmlRpc.Client.Serializer.Parser;

namespace XmlRpc.Client.Serializer.Response
{
    public class XmlRpcResponseDeserializer : XmlRpcSerializer
    {
        public XmlRpcResponse DeserializeResponse(Stream inputStream, Type serviceType)
        {
            if (Configuration.AllowInvalidHTTPContent())
            {
                inputStream = CopyStream(inputStream);
                RemoveLineBreaks(inputStream); // why are we doing this?
            }

            var xdoc = XmlDocumentLoader.LoadXmlDocument(inputStream);
            return DeserializeResponse(xdoc, serviceType);
        }

        public XmlRpcResponse DeserializeResponse(XmlDocument xdoc, Type returnType)
        {
            var parser = new XmlParser(Configuration);
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

            var response = new XmlRpcResponse { retVal = null };
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
            var newStream = new MemoryStream();
            inputStream.CopyTo(newStream);
            newStream.Seek(0, SeekOrigin.Begin);

            return newStream;
        }

        void RemoveLineBreaks(Stream inputStream)
        {
            while (true)
            {
                var singleByte = inputStream.ReadByte();
                if (singleByte == -1)
                    throw new XmlRpcIllFormedXmlException("Response from server does not contain valid XML.");

                if (singleByte != 0x0d && singleByte != 0x0a && singleByte != ' ' && singleByte != '\t')
                {
                    inputStream.Position -= 1;
                    break;
                }
            }
        }
    }
}
