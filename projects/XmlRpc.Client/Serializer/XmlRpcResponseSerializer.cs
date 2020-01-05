using System.IO;
using System.Xml;
using XmlRpc.Client.Exceptions;
using XmlRpc.Client.Model;

namespace XmlRpc.Client.Serializer
{
    public class XmlRpcResponseSerializer : XmlRpcSerializer
    {
        public void SerializeResponse(Stream inputStream, XmlRpcResponse response)
        {
            var returnValue = response.retVal;
            if (returnValue is XmlRpcFaultException)
            {
                SerializeFaultResponse(inputStream, (XmlRpcFaultException)returnValue);
                return;
            }

            var xtw = new XmlTextWriter(inputStream, Configuration.XmlEncoding);
            Configuration.ConfigureXmlFormat(xtw);

            xtw.WriteStartDocument();
            xtw.WriteStartElement("", "methodResponse", "");
            xtw.WriteStartElement("", "params", "");
            xtw.WriteStartElement("", "param", "");

            // "void" methods actually return an empty value
            if (returnValue == null)
                WriteVoidValue(xtw);
            else
                WriteReturnValue(xtw, returnValue);

            xtw.WriteEndElement();
            xtw.WriteEndElement();
            xtw.WriteEndElement();
            xtw.Flush();
        }

        void WriteReturnValue(XmlTextWriter xtw, object returnValue)
        {
            try
            {
                Serialize(xtw, returnValue);
            }
            catch (XmlRpcUnsupportedTypeException ex)
            {
                throw new XmlRpcInvalidReturnType($"Return value is of, or contains an instance of, type {ex.UnsupportedType} which cannot be mapped to an XML-RPC type");
            }
        }

        void WriteVoidValue(XmlTextWriter xtw)
        {
            xtw.WriteStartElement("", "value", "");
            xtw.WriteEndElement();
        }
    }
}
