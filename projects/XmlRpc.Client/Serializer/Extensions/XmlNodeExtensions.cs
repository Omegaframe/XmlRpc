using System;
using System.Linq;
using System.Xml;

namespace XmlRpc.Client.Serializer.Extensions
{
    static class XmlNodeExtensions
    {
        public static XmlNode[] SelectChildNodes(this XmlNode node, string name)
        {
            return node.ChildNodes.Cast<XmlNode>()
                .Where(n => n.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        public static XmlNode SelectValueNode(this XmlNode valueNode)
        {
            // an XML-RPC value is either held as the child node of a <value> element
            // or is just the text of the value node as an implicit string value
            var vvNode = valueNode.SelectSingleNode("*");
            if (vvNode == null)
                vvNode = valueNode.FirstChild;

            return vvNode;
        }

        public static Tuple<XmlNode, bool> SelectPossibleDoupletteNode(this XmlNode node, string name)
        {
            var childNodes = node.ChildNodes.Cast<XmlNode>()
                 .Where(n => n.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                 .ToArray();

            return new Tuple<XmlNode, bool>(childNodes.FirstOrDefault(), childNodes.Length > 1);
        }
    }
}
