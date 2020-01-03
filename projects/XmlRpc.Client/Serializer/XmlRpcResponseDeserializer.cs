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
            var response = new XmlRpcResponse();
            var methodResponseNode = SelectSingleNode(xdoc, "methodResponse");
            if (methodResponseNode == null)
                throw new XmlRpcInvalidXmlRpcException("Response XML not valid XML-RPC - missing methodResponse element.");

            var faultNode = SelectSingleNode(methodResponseNode, "fault");
            if (faultNode != null)
            {
                var parseStack = new ParseStack("fault response");
                var faultEx = ParseFault(faultNode, parseStack, Configuration.MappingAction);
                throw faultEx;
            }

            var paramsNode = SelectSingleNode(methodResponseNode, "params");
            if (paramsNode == null && returnType != null)
            {
                if (returnType == typeof(void))
                    return new XmlRpcResponse(null);
                else
                    throw new XmlRpcInvalidXmlRpcException("Response XML not valid XML-RPC - missing params element.");
            }

            var paramNode = SelectSingleNode(paramsNode, "param");
            if (paramNode == null && returnType != null)
            {
                if (returnType == typeof(void))
                    return new XmlRpcResponse(null);
                else
                    throw new XmlRpcInvalidXmlRpcException("Response XML not valid XML-RPC - missing params element.");
            }

            var valueNode = SelectSingleNode(paramNode, "value");
            if (valueNode == null)            
                throw new XmlRpcInvalidXmlRpcException("Response XML not valid XML-RPC - missing value element.");

            response.retVal = null;
            if (returnType != typeof(void))          
            {
                var parseStack = new ParseStack("response");                
                var node = SelectValueNode(valueNode);
                response.retVal = ParseValue(node, returnType, parseStack, Configuration.MappingAction);
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
