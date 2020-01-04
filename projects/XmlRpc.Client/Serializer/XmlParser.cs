using System;
using System.Xml;
using XmlRpc.Client.DataTypes;
using XmlRpc.Client.Exceptions;
using XmlRpc.Client.Model;

namespace XmlRpc.Client.Serializer
{
    class XmlParser
    {
        public object ParseInt(XmlNode node, Type valueType, ParseStack parseStack)
        {
            if (valueType.IsNoInteger())
                throw new XmlRpcTypeMismatchException(parseStack.ParseType + " contains int value where " + XmlRpcServiceInfo.GetXmlRpcTypeString(valueType) + " expected " + parseStack.Dump());

            parseStack.Push("integer");
            try
            {
                var valueNode = node.FirstChild;
                if (valueNode == null)
                    throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType + " contains invalid int element " + parseStack.Dump());

                var strValue = valueNode.Value;
                if (!int.TryParse(strValue, out var parseResult))
                    throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType + " contains invalid int value " + parseStack.Dump());

                return valueType == typeof(XmlRpcInt) ? new XmlRpcInt(parseResult) : (object)parseResult;
            }
            finally
            {
                parseStack.Pop();
            }
        }

        public object ParseLong(XmlNode node, Type valueType, ParseStack parseStack)
        {
            if (valueType.IsNoLong())
                throw new XmlRpcTypeMismatchException(parseStack.ParseType + " contains i8 value where " + XmlRpcServiceInfo.GetXmlRpcTypeString(valueType) + " expected " + parseStack.Dump());

            parseStack.Push("i8");
            try
            {
                var valueNode = node.FirstChild;
                if (valueNode == null)
                    throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType + " contains invalid i8 element " + parseStack.Dump());

                var strValue = valueNode.Value;
                if (!long.TryParse(strValue, out var parseResult))
                    throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType + " contains invalid i8 value " + parseStack.Dump());

                return parseResult;
            }
            finally
            {
                parseStack.Pop();
            }
        }

        public object ParseString(XmlNode node, Type valueType, ParseStack parseStack)
        {
            if (valueType.IsNoString())
                throw new XmlRpcTypeMismatchException(parseStack.ParseType + " contains string value where " + XmlRpcServiceInfo.GetXmlRpcTypeString(valueType) + " expected " + parseStack.Dump());

            parseStack.Push("string");
            try
            {
                return node.FirstChild == null ? string.Empty : node.FirstChild.Value;
            }
            finally
            {
                parseStack.Pop();
            }
        }

        public object ParseBoolean(XmlNode node, Type valueType, ParseStack parseStack)
        {
            if (valueType.IsNoBoolean())
                throw new XmlRpcTypeMismatchException(parseStack.ParseType + " contains boolean value where " + XmlRpcServiceInfo.GetXmlRpcTypeString(valueType) + " expected " + parseStack.Dump());

            parseStack.Push("boolean");
            try
            {
                var textbool = node.FirstChild.Value;
                if (!bool.TryParse(textbool, out var parseResult))
                    throw new XmlRpcInvalidXmlRpcException("reponse contains invalid boolean value " + parseStack.Dump());

                return valueType == typeof(XmlRpcBoolean) ? new XmlRpcBoolean(parseResult) : (object)parseResult;
            }
            finally
            {
                parseStack.Pop();
            }
        }

        public object ParseDouble(XmlNode node, Type ValueType, ParseStack parseStack)
        {
            if (ValueType.IsNoDouble())
                throw new XmlRpcTypeMismatchException(parseStack.ParseType + " contains double value where " + XmlRpcServiceInfo.GetXmlRpcTypeString(ValueType) + " expected " + parseStack.Dump());

            parseStack.Push("double");
            try
            {
                if (!double.TryParse(node.FirstChild.Value, out var parseResult))
                    throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType + " contains invalid double value " + parseStack.Dump());

                return ValueType == typeof(XmlRpcDouble) ? new XmlRpcDouble(parseResult) : (object)parseResult;
            }
            finally
            {
                parseStack.Pop();
            }
        }

        public object ParseDateTime(XmlNode node, Type valueType, ParseStack parseStack, bool mapEmptyDateTimeToMinValue, bool mapZerosDateTimeToMinValue)
        {
            if (valueType.IsNoDateTime())
                throw new XmlRpcTypeMismatchException(parseStack.ParseType + " contains dateTime.iso8601 value where " + XmlRpcServiceInfo.GetXmlRpcTypeString(valueType) + " expected " + parseStack.Dump());

            parseStack.Push("dateTime");
            try
            {
                var child = node.FirstChild;
                if (child == null)
                {
                    if (mapEmptyDateTimeToMinValue)
                        return DateTime.MinValue;
                    else
                        throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType + " contains empty dateTime value " + parseStack.Dump());
                }

                var datestring = child.Value;

                if (!DateTime8601.TryParseDateTime8601(datestring, out var retVal))
                {
                    if (mapZerosDateTimeToMinValue
                        && datestring.StartsWith("0000")
                        && (datestring == "00000000T00:00:00" || datestring == "0000-00-00T00:00:00Z" || datestring == "00000000T00:00:00Z" || datestring == "0000-00-00T00:00:00"))
                        retVal = DateTime.MinValue;
                    else
                        throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType + " contains invalid dateTime value " + parseStack.Dump());
                }

                return valueType == typeof(XmlRpcDateTime) ? new XmlRpcDateTime(retVal) : (object)retVal;
            }
            finally
            {
                parseStack.Pop();
            }
        }

        public object ParseBase64(XmlNode node, Type valueType, ParseStack parseStack)
        {
            if (valueType.IsNoByteArray())
                throw new XmlRpcTypeMismatchException(parseStack.ParseType + " contains base64 value where " + XmlRpcServiceInfo.GetXmlRpcTypeString(valueType) + " expected " + parseStack.Dump());

            parseStack.Push("base64");
            try
            {
                if (node.FirstChild == null)
                    return new byte[0];

                var base64String = node.FirstChild.Value;
                var buffer = new Span<byte>();

                if (!Convert.TryFromBase64String(base64String, buffer, out _))
                    throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType + " contains invalid base64 value " + parseStack.Dump());

                return buffer.ToArray();
            }
            finally
            {
                parseStack.Pop();
            }
        }
    }
}
