using System;
using System.IO;
using System.Xml;
using XmlRpc.Client.Exceptions;

namespace XmlRpc.Client.Serializer
{
    static class XmlDocumentLoader
    {
        public static XmlDocument LoadXmlDocument(Stream inputStream)
        {
            try
            {
                using (var xmlRdr = new XmlTextReader(inputStream) { DtdProcessing = DtdProcessing.Prohibit })
                {
                    var xdoc = new XmlDocument { PreserveWhitespace = true };
                    xdoc.Load(xmlRdr);

                    return xdoc;
                }
            }
            catch (Exception ex)
            {
                throw new XmlRpcIllFormedXmlException("Request from client does not contain valid XML.", ex);
            }
        }
    }
}
