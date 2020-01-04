using System;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Xml;
using XmlRpc.Client.Attributes;
using XmlRpc.Client.DataTypes;
using XmlRpc.Client.Exceptions;
using XmlRpc.Client.Model;

namespace XmlRpc.Client.Serializer
{
    class XmlParser
    {
        readonly SerializerConfig _config;

        public XmlParser(SerializerConfig serializerConfig)
        {
            _config = serializerConfig;
        }

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

        public object ParseDateTime(XmlNode node, Type valueType, ParseStack parseStack)
        {
            if (valueType.IsNoDateTime())
                throw new XmlRpcTypeMismatchException(parseStack.ParseType + " contains dateTime.iso8601 value where " + XmlRpcServiceInfo.GetXmlRpcTypeString(valueType) + " expected " + parseStack.Dump());

            parseStack.Push("dateTime");
            try
            {
                var child = node.FirstChild;
                if (child == null)
                {
                    if (_config.MapEmptyDateTimeToMinValue())
                        return DateTime.MinValue;
                    else
                        throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType + " contains empty dateTime value " + parseStack.Dump());
                }

                var datestring = child.Value;

                if (!DateTime8601.TryParseDateTime8601(datestring, out var retVal))
                {
                    if (_config.MapZerosDateTimeToMinValue()
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

        public object ParseHashtable(XmlNode node, ParseStack parseStack)
        {
            var retObj = new XmlRpcStruct();
            parseStack.Push("class mapped to XmlRpcStruct");
            try
            {
                var members = node.SelectChildNodes("member");
                foreach (var member in members)
                {
                    var (nameNode, hasMultipleNameNodes) = member.SelectPossibleDoupletteNode("name");
                    var (valueNode, hasMultipleValueNodes) = member.SelectPossibleDoupletteNode("value");

                    if (nameNode == null || nameNode.FirstChild == null)
                        throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType + " contains a member with missing name" + " " + parseStack.Dump());
                    if (hasMultipleNameNodes)
                        throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType + " contains member with more than one name element" + " " + parseStack.Dump());

                    var rpcName = nameNode.FirstChild.Value;
                    if (valueNode == null)
                        throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType + " contains class member " + rpcName + " with missing value " + " " + parseStack.Dump());
                    if (hasMultipleValueNodes)
                        throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType + " contains member with more than one value element" + " " + parseStack.Dump());

                    if (retObj.Contains(rpcName))
                    {
                        if (_config.IgnoreDuplicateMembers())
                            continue;

                        throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType + " contains class value with duplicate member " + nameNode.FirstChild.Value + " " + parseStack.Dump());
                    }

                    parseStack.Push($"member {rpcName}");
                    try
                    {
                        var vvNode = valueNode.SelectValueNode();
                        var valObj = ParseValue(vvNode, null, parseStack);
                        retObj.Add(rpcName, valObj);
                    }
                    finally
                    {
                        parseStack.Pop();
                    }

                }
            }
            finally
            {
                parseStack.Pop();
            }
            return retObj;
        }


        public object ParseValue(XmlNode node, Type ValueType, ParseStack parseStack)
        {
            return ParseValue(node, ValueType, parseStack, out _, out _);
        }

        object ParseValue(XmlNode node, Type ValueType, ParseStack parseStack, out Type ParsedType, out Type ParsedArrayType)
        {
            ParsedType = null;
            ParsedArrayType = null;
            // if suppplied type is System.object then ignore it because
            // if doesn't provide any useful information (parsing methods
            // expect null in this case)
            Type valType = ValueType;
            if (valType != null && valType.BaseType == null)
                valType = null;

            object retObj;
            if (node == null)
            {
                retObj = "";
            }
            else if (node is XmlText || node is XmlWhitespace)
            {
                if (valType != null && valType != typeof(string))
                    throw new XmlRpcTypeMismatchException(parseStack.ParseType + " contains implicit string value where " + XmlRpcServiceInfo.GetXmlRpcTypeString(valType) + " expected " + parseStack.Dump());

                retObj = node.Value;
            }
            else
            {
                if (node.Name == "array")
                    retObj = ParseArray(node, valType, parseStack);
                else if (node.Name == "base64")
                    retObj = ParseBase64(node, valType, parseStack);
                else if (node.Name == "struct")
                {
                    // if we don't know the expected class type then we must
                    // parse the XML-RPC class as an instance of XmlRpcStruct
                    if (valType != null && valType != typeof(XmlRpcStruct) && !valType.IsSubclassOf(typeof(XmlRpcStruct)))
                        retObj = ParseStruct(node, valType, parseStack);
                    else
                        retObj = ParseHashtable(node, parseStack);
                }
                else if (node.Name == "i4" || node.Name == "int") // integer has two representations in XML-RPC spec
                {
                    retObj = ParseInt(node, valType, parseStack);
                    ParsedType = typeof(int);
                    ParsedArrayType = typeof(int[]);
                }
                else if (node.Name == "i8")
                {
                    retObj = ParseLong(node, valType, parseStack);
                    ParsedType = typeof(long);
                    ParsedArrayType = typeof(long[]);
                }
                else if (node.Name == "string")
                {
                    retObj = ParseString(node, valType, parseStack);
                    ParsedType = typeof(string);
                    ParsedArrayType = typeof(string[]);
                }
                else if (node.Name == "boolean")
                {
                    retObj = ParseBoolean(node, valType, parseStack);
                    ParsedType = typeof(bool);
                    ParsedArrayType = typeof(bool[]);
                }
                else if (node.Name == "double")
                {
                    retObj = ParseDouble(node, valType, parseStack);
                    ParsedType = typeof(double);
                    ParsedArrayType = typeof(double[]);
                }
                else if (node.Name == "dateTime.iso8601")
                {
                    retObj = ParseDateTime(node, valType, parseStack);
                    ParsedType = typeof(DateTime);
                    ParsedArrayType = typeof(DateTime[]);
                }
                else
                {
                    throw new XmlRpcInvalidXmlRpcException("Invalid value element: <" + node.Name + ">");
                }
            }

            return retObj;
        }

        object ParseArray(XmlNode node, Type valueType, ParseStack parseStack)
        {
            // required type must be an array
            if (valueType != null
              && !(valueType.IsArray == true
                  || valueType == typeof(Array)
                  || valueType == typeof(object)))
            {
                throw new XmlRpcTypeMismatchException(parseStack.ParseType
                  + " contains array value where "
                  + XmlRpcServiceInfo.GetXmlRpcTypeString(valueType)
                  + " expected " + parseStack.Dump());
            }
            if (valueType != null)
            {
                XmlRpcType xmlRpcType = XmlRpcServiceInfo.GetXmlRpcType(valueType);
                if (xmlRpcType == XmlRpcType.tMultiDimArray)
                {
                    parseStack.Push("array mapped to type " + valueType.Name);
                    object ret = ParseMultiDimArray(node, valueType, parseStack);
                    return ret;
                }
                parseStack.Push("array mapped to type " + valueType.Name);
            }
            else
                parseStack.Push("array");
            var dataNode = node.SelectSingleNode("data");
            var childNodes = dataNode.SelectChildNodes("value");
            var nodeCount = childNodes.Length;
            var elements = new object[nodeCount];
            // determine type of array elements
            Type elemType;
            if (valueType != null
              && valueType != typeof(Array)
              && valueType != typeof(object))
            {

                elemType = valueType.GetElementType();
            }
            else
            {
                elemType = typeof(object);
            }
            bool bGotType = false;
            Type useType = null;
            int i = 0;
            foreach (XmlNode vNode in childNodes)
            {
                parseStack.Push(String.Format("element {0}", i));
                XmlNode vvNode = vNode.SelectValueNode();
                elements[i++] = ParseValue(vvNode, elemType, parseStack,
                                            out Type parsedType, out Type parsedArrayType);
                if (bGotType == false)
                {
                    useType = parsedArrayType;
                    bGotType = true;
                }
                else
                {
                    if (useType != parsedArrayType)
                        useType = null;
                }
                parseStack.Pop();
            }
            object[] args = new object[1]; args[0] = nodeCount;
            object retObj;
            if (valueType != null
              && valueType != typeof(Array)
              && valueType != typeof(object))
            {
                retObj = Activator.CreateInstance(valueType, args);
            }
            else
            {
                if (useType == null)
                    retObj = Activator.CreateInstance(typeof(object[]), args);
                else
                    retObj = Activator.CreateInstance(useType, args);
            }
            for (int j = 0; j < elements.Length; j++)
            {
                ((Array)retObj).SetValue(elements[j], j);
            }
            parseStack.Pop();
            return retObj;
        }

        object ParseMultiDimArray(XmlNode node, Type ValueType,
          ParseStack parseStack)
        {
            // parse the type name to get element type and array rank

            Type elemType = ValueType.GetElementType();
            int rank = ValueType.GetArrayRank();

            // elements will be stored sequentially as nested arrays are parsed
            ArrayList elements = new ArrayList();
            // create array to store length of each dimension - initialize to 
            // all zeroes so that when parsing we can determine if an array for 
            // that dimension has been parsed already
            int[] dimLengths = new int[rank];
            dimLengths.Initialize();
            ParseMultiDimElements(node, rank, 0, elemType, elements, dimLengths,
              parseStack);
            // build arguments to define array dimensions and create the array
            object[] args = new object[dimLengths.Length];
            for (int argi = 0; argi < dimLengths.Length; argi++)
            {
                args[argi] = dimLengths[argi];
            }
            Array ret = (Array)Activator.CreateInstance(ValueType, args);
            // copy elements into new multi-dim array
            //!! make more efficient
            int length = ret.Length;
            for (int e = 0; e < length; e++)
            {
                int[] indices = new int[dimLengths.Length];
                int div = 1;
                for (int f = (indices.Length - 1); f >= 0; f--)
                {
                    indices[f] = (e / div) % dimLengths[f];
                    div *= dimLengths[f];
                }
                ret.SetValue(elements[e], indices);
            }
            return ret;
        }

        void ParseMultiDimElements(XmlNode node, int Rank, int CurRank,
          Type elemType, ArrayList elements, int[] dimLengths,
          ParseStack parseStack)
        {
            if (node.Name != "array")
            {
                throw new XmlRpcTypeMismatchException(
                  "param element does not contain array element.");
            }
            XmlNode dataNode = node.SelectSingleNode("data");
            XmlNode[] childNodes = dataNode.SelectChildNodes("value");
            int nodeCount = childNodes.Length;
            //!! check that multi dim array is not jagged
            if (dimLengths[CurRank] != 0 && nodeCount != dimLengths[CurRank])
            {
                throw new XmlRpcNonRegularArrayException(
                  "Multi-dimensional array must not be jagged.");
            }
            dimLengths[CurRank] = nodeCount;  // in case first array at this rank
            if (CurRank < (Rank - 1))
            {
                foreach (XmlNode vNode in childNodes)
                {
                    XmlNode arrayNode = vNode.SelectSingleNode("array");
                    ParseMultiDimElements(arrayNode, Rank, CurRank + 1, elemType,
                      elements, dimLengths, parseStack);
                }
            }
            else
            {
                foreach (XmlNode vNode in childNodes)
                {
                    XmlNode vvNode = vNode.SelectValueNode();
                    elements.Add(ParseValue(vvNode, elemType, parseStack));
                }
            }
        }

        object ParseStruct(
          XmlNode node,
          Type valueType,
          ParseStack parseStack)
        {
            if (valueType.IsPrimitive)
            {
                throw new XmlRpcTypeMismatchException(parseStack.ParseType
                  + " contains class value where "
                  + XmlRpcServiceInfo.GetXmlRpcTypeString(valueType)
                  + " expected " + parseStack.Dump());
            }

            if (valueType.IsGenericType
              && valueType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                valueType = valueType.GetGenericArguments()[0];
            }

            object retObj;
            try
            {
                retObj = Activator.CreateInstance(valueType);
            }
            catch (Exception)
            {
                throw new XmlRpcTypeMismatchException(parseStack.ParseType
                  + " contains class value where "
                  + XmlRpcServiceInfo.GetXmlRpcTypeString(valueType)
                  + " expected (as type " + valueType.Name + ") "
                  + parseStack.Dump());
            }
            // Note: mapping action on a class is only applied locally - it 
            // does not override the global mapping action when members of the 
            // class are parsed
            MappingAction localAction = _config.MappingAction;
            if (valueType != null)
            {
                parseStack.Push("class mapped to type " + valueType.Name);
                localAction = StructMappingAction(valueType, localAction);
            }
            else
            {
                parseStack.Push("struct");
            }
            // create map of field names and remove each name from it as 
            // processed so we can determine which fields are missing
            // TODO: replace HashTable with lighter collection
            Hashtable names = new Hashtable();
            foreach (FieldInfo fi in valueType.GetFields())
            {
                if (Attribute.IsDefined(fi, typeof(NonSerializedAttribute)))
                    continue;
                names.Add(fi.Name, fi.Name);
            }
            foreach (PropertyInfo pi in valueType.GetProperties())
            {
                if (Attribute.IsDefined(pi, typeof(NonSerializedAttribute)))
                    continue;
                names.Add(pi.Name, pi.Name);
            }
            XmlNode[] members = node.SelectChildNodes("member");
            int fieldCount = 0;
            foreach (XmlNode member in members)
            {
                if (member.Name != "member")
                    continue;

                var (nameNode, nameIsDuplicated) = member.SelectPossibleDoupletteNode("name");
                var (valueNode, valueIsDuplicated) = member.SelectPossibleDoupletteNode("value");

                if (nameNode == null || nameNode.FirstChild == null)
                    throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType
                      + " contains a member with missing name"
                      + " " + parseStack.Dump());
                if (nameIsDuplicated)
                    throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType
                      + " contains member with more than one name element"
                      + " " + parseStack.Dump());
                string name = nameNode.FirstChild.Value;
                if (valueNode == null)
                    throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType
                      + " contains class member " + name + " with missing value "
                      + " " + parseStack.Dump());
                if (valueIsDuplicated)
                    throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType
                      + " contains member with more than one value element"
                      + " " + parseStack.Dump());
                string structName = GetStructName(valueType, name);
                if (structName != null)
                    name = structName;
                MemberInfo mi = valueType.GetField(name);
                if (mi == null)
                    mi = valueType.GetProperty(name);
                if (mi == null)
                    continue;
                if (names.Contains(name))
                    names.Remove(name);
                else
                {
                    if (Attribute.IsDefined(mi, typeof(NonSerializedAttribute)))
                    {
                        parseStack.Push(String.Format("member {0}", name));
                        throw new XmlRpcNonSerializedMember("Cannot map XML-RPC class "
                          + "member onto member marked as [NonSerialized]: "
                          + " " + parseStack.Dump());
                    }
                    if (!_config.IgnoreDuplicateMembers())
                        throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType
                          + " contains class value with duplicate member "
                          + nameNode.FirstChild.Value
                          + " " + parseStack.Dump());
                    else
                        continue;   // ignore duplicate member
                }
                object valObj = null;
                switch (mi.MemberType)
                {
                    case MemberTypes.Field:
                        FieldInfo fi = (FieldInfo)mi;
                        if (valueType == null)
                            parseStack.Push(String.Format("member {0}", name));
                        else
                            parseStack.Push(String.Format("member {0} mapped to type {1}",
                              name, fi.FieldType.Name));
                        try
                        {
                            XmlNode vvvNode = valueNode.SelectValueNode();
                            valObj = ParseValue(vvvNode, fi.FieldType,
                              parseStack);
                        }
                        catch (XmlRpcInvalidXmlRpcException)
                        {
                            if (valueType != null && localAction == MappingAction.Error)
                            {
                                MappingAction memberAction = MemberMappingAction(valueType,
                                  name, MappingAction.Error);
                                if (memberAction == MappingAction.Error)
                                    throw;
                            }
                        }
                        finally
                        {
                            parseStack.Pop();
                        }
                        fi.SetValue(retObj, valObj);
                        break;
                    case MemberTypes.Property:
                        PropertyInfo pi = (PropertyInfo)mi;
                        if (valueType == null)
                            parseStack.Push(String.Format("member {0}", name));
                        else

                            parseStack.Push(String.Format("member {0} mapped to type {1}",
                              name, pi.PropertyType.Name));
                        XmlNode vvNode = valueNode.SelectValueNode();
                        valObj = ParseValue(vvNode, pi.PropertyType,
                          parseStack);
                        parseStack.Pop();

                        pi.SetValue(retObj, valObj, null);
                        break;
                }
                fieldCount++;
            }
            if (localAction == MappingAction.Error && names.Count > 0)
                ReportMissingMembers(valueType, names, parseStack);
            parseStack.Pop();
            return retObj;
        }

        public XmlRpcFaultException ParseFault(XmlNode faultNode, ParseStack parseStack)
        {
            var valueNode = faultNode.SelectSingleNode("value");
            var structNode = valueNode.SelectSingleNode("struct");
            if (structNode == null)
                throw new XmlRpcInvalidXmlRpcException("class element missing from fault response.");

            var fault = new XmlFault();

            try
            {
                fault = (XmlFault)ParseValue(structNode, typeof(XmlFault), parseStack);
            }
            catch (Exception ex)
            {
                // some servers incorrectly return fault code in a string
                if (!_config.AllowStringFaultCode())
                    throw;

                try
                {
                    var faultStrCode = (FaultStructStringCode)ParseValue(structNode, typeof(FaultStructStringCode), parseStack);
                    fault.faultCode = Convert.ToInt32(faultStrCode.faultCode);
                    fault.faultString = faultStrCode.faultString;
                }
                catch (Exception)
                {
                    // use exception from when attempting to parse code as integer
                    throw ex;
                }
            }

            return new XmlRpcFaultException(fault.faultCode, fault.faultString);
        }

        void ReportMissingMembers(Type valueType, Hashtable names, ParseStack parseStack)
        {
            var sb = new StringBuilder();
            var errorCount = 0;
            var sep = string.Empty;

            foreach (string key in names.Keys)
            {
                var memberAction = MemberMappingAction(valueType, key, MappingAction.Error);
                if (memberAction == MappingAction.Error)
                {
                    sb.Append(sep);
                    sb.Append(key);
                    sep = " ";
                    errorCount++;
                }
            }

            if (errorCount == 0)
                return;

            var plural = string.Empty;
            if (errorCount > 1)
                plural = "s";

            throw new XmlRpcTypeMismatchException(parseStack.ParseType + " contains class value with missing non-optional member" + plural + ": " + sb.ToString() + " " + parseStack.Dump());
        }

        public MappingAction MemberMappingAction(Type type, string memberName, MappingAction currentAction)
        {
            if (type == null)
                return currentAction;

            var fi = type.GetField(memberName);
            Attribute attr;
            if (fi != null)
            {
                attr = Attribute.GetCustomAttribute(fi, typeof(XmlRpcMissingMappingAttribute));
            }
            else
            {
                PropertyInfo pi = type.GetProperty(memberName);
                attr = Attribute.GetCustomAttribute(pi, typeof(XmlRpcMissingMappingAttribute));
            }

            return ((XmlRpcMissingMappingAttribute)attr)?.Action ?? currentAction;
        }

        public MappingAction StructMappingAction(Type type, MappingAction currentAction)
        {
            if (type == null)
                return currentAction;

            var attr = Attribute.GetCustomAttribute(type, typeof(XmlRpcMissingMappingAttribute));
            return ((XmlRpcMissingMappingAttribute)attr)?.Action ?? currentAction;
        }

        string GetStructName(Type ValueType, string XmlRpcName)
        {
            if (ValueType == null)
                return null;

            foreach (var fi in ValueType.GetFields())
            {
                var attr = Attribute.GetCustomAttribute(fi, typeof(XmlRpcMemberAttribute));
                if (attr != null && attr is XmlRpcMemberAttribute && ((XmlRpcMemberAttribute)attr).Member.Equals(XmlRpcName))
                {
                    string ret = fi.Name;
                    return ret;
                }
            }

            foreach (var pi in ValueType.GetProperties())
            {
                var attr = Attribute.GetCustomAttribute(pi, typeof(XmlRpcMemberAttribute));
                if (attr != null && attr is XmlRpcMemberAttribute && ((XmlRpcMemberAttribute)attr).Member.Equals(XmlRpcName))
                {
                    string ret = pi.Name;
                    return ret;
                }
            }

            return null;
        }
    }
}
