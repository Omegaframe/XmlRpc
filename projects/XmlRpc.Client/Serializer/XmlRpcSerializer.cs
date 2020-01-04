using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using XmlRpc.Client.Attributes;
using XmlRpc.Client.DataTypes;
using XmlRpc.Client.Exceptions;
using XmlRpc.Client.Model;

namespace XmlRpc.Client.Serializer
{
    public class XmlRpcSerializer
    {
        public SerializerConfig Configuration { get; }

        public XmlRpcSerializer()
        {
            Configuration = new SerializerConfig();
        }

        public void Serialize(XmlTextWriter xtw, object o, MappingAction mappingAction)
        {
            Serialize(xtw, o, mappingAction, new ArrayList(16));
        }

        public void Serialize(XmlTextWriter xtw, object o, MappingAction mappingAction, ArrayList nestedObjs)
        {
            if (nestedObjs.Contains(o))
                throw new XmlRpcUnsupportedTypeException(nestedObjs[0].GetType(), "Cannot serialize recursive data structure");

            try
            {
                nestedObjs.Add(o);
                TrySerialize(xtw, o, mappingAction, nestedObjs);
            }
            catch (System.NullReferenceException)
            {
                throw new XmlRpcNullReferenceException("Attempt to serialize data containing null reference");
            }
            finally
            {
                nestedObjs.RemoveAt(nestedObjs.Count - 1);
            }
        }

        void TrySerialize(XmlTextWriter xtw, object targetObject, MappingAction mappingAction, ArrayList nestedObjs)
        {
            xtw.WriteStartElement("", "value", "");
            var xType = XmlRpcServiceInfo.GetXmlRpcType(targetObject.GetType());

            if (xType == XmlRpcType.tArray)
            {
                xtw.WriteStartElement("", "array", "");
                xtw.WriteStartElement("", "data", "");

                foreach (var aobj in (Array)targetObject)
                {
                    if (aobj == null)
                        throw new XmlRpcMappingSerializeException($"Items in array cannot be null ({targetObject.GetType().GetElementType()}[]).");

                    Serialize(xtw, aobj, mappingAction, nestedObjs);
                }

                xtw.WriteEndElement();
                xtw.WriteEndElement();
            }
            else if (xType == XmlRpcType.tMultiDimArray)
            {
                var mda = (Array)targetObject;
                var indices = new int[mda.Rank];
                BuildArrayXml(xtw, mda, 0, indices, mappingAction, nestedObjs);
            }
            else if (xType == XmlRpcType.tBase64)
            {
                var buf = (byte[])targetObject;
                xtw.WriteStartElement("", "base64", "");
                xtw.WriteBase64(buf, 0, buf.Length);
                xtw.WriteEndElement();
            }
            else if (xType == XmlRpcType.tBoolean)
            {
                bool boolVal;
                if (targetObject is bool)
                    boolVal = (bool)targetObject;
                else
                    boolVal = (XmlRpcBoolean)targetObject;

                if (boolVal)
                    xtw.WriteElementString("boolean", "1");
                else
                    xtw.WriteElementString("boolean", "0");
            }
            else if (xType == XmlRpcType.tDateTime)
            {
                DateTime dt;
                if (targetObject is DateTime)
                    dt = (DateTime)targetObject;
                else
                    dt = (XmlRpcDateTime)targetObject;

                var sdt = dt.ToString("yyyyMMdd'T'HH':'mm':'ss", DateTimeFormatInfo.InvariantInfo);
                xtw.WriteElementString("dateTime.iso8601", sdt);
            }
            else if (xType == XmlRpcType.tDouble)
            {
                double doubleVal;
                if (targetObject is double)
                    doubleVal = (double)targetObject;
                else
                    doubleVal = (XmlRpcDouble)targetObject;

                xtw.WriteElementString("double", doubleVal.ToString(null, CultureInfo.InvariantCulture));
            }
            else if (xType == XmlRpcType.tHashtable)
            {
                xtw.WriteStartElement("", "struct", "");
                var xrs = targetObject as XmlRpcStruct;

                foreach (object obj in xrs.Keys)
                {
                    var skey = obj as string;
                    xtw.WriteStartElement("", "member", "");
                    xtw.WriteElementString("name", skey);

                    Serialize(xtw, xrs[skey], mappingAction, nestedObjs);

                    xtw.WriteEndElement();
                }

                xtw.WriteEndElement();
            }
            else if (xType == XmlRpcType.tInt32)
            {
                if (Configuration.UseIntTag)
                    xtw.WriteElementString("int", targetObject.ToString());
                else
                    xtw.WriteElementString("i4", targetObject.ToString());
            }
            else if (xType == XmlRpcType.tInt64)
            {
                xtw.WriteElementString("i8", targetObject.ToString());
            }
            else if (xType == XmlRpcType.tString)
            {
                if (Configuration.UseStringTag)
                    xtw.WriteElementString("string", (string)targetObject);
                else
                    xtw.WriteString((string)targetObject);
            }
            else if (xType == XmlRpcType.tStruct)
            {
                xtw.WriteStartElement("", "struct", "");
                var mis = targetObject.GetType().GetMembers();
                var structAction = StructMappingAction(targetObject.GetType(), mappingAction);

                foreach (var mi in mis)
                {
                    if (Attribute.IsDefined(mi, typeof(NonSerializedAttribute)))
                        continue;

                    if (mi.MemberType == MemberTypes.Field)
                    {
                        var fi = (FieldInfo)mi;
                        var member = fi.Name;
                        var attrchk = Attribute.GetCustomAttribute(fi, typeof(XmlRpcMemberAttribute));
                        if (attrchk != null && attrchk is XmlRpcMemberAttribute)
                        {
                            var mmbr = ((XmlRpcMemberAttribute)attrchk).Member;
                            if (mmbr != "")
                                member = mmbr;
                        }

                        if (fi.GetValue(targetObject) == null)
                        {
                            var memberAction = MemberMappingAction(targetObject.GetType(), fi.Name, structAction);
                            if (memberAction == MappingAction.Ignore)
                                continue;

                            throw new XmlRpcMappingSerializeException(@"Member """ + member + @""" of class """ + targetObject.GetType().Name + @""" cannot be null.");
                        }

                        xtw.WriteStartElement("", "member", "");
                        xtw.WriteElementString("name", member);

                        Serialize(xtw, fi.GetValue(targetObject), mappingAction, nestedObjs);

                        xtw.WriteEndElement();
                    }
                    else if (mi.MemberType == MemberTypes.Property)
                    {
                        var pi = (PropertyInfo)mi;
                        var member = pi.Name;
                        var attrchk = Attribute.GetCustomAttribute(pi, typeof(XmlRpcMemberAttribute));

                        if (attrchk != null && attrchk is XmlRpcMemberAttribute)
                        {
                            var mmbr = ((XmlRpcMemberAttribute)attrchk).Member;
                            if (mmbr != "")
                                member = mmbr;
                        }

                        if (pi.GetValue(targetObject) == null)
                        {
                            var memberAction = MemberMappingAction(targetObject.GetType(), pi.Name, structAction);
                            if (memberAction == MappingAction.Ignore)
                                continue;
                        }

                        xtw.WriteStartElement("", "member", "");
                        xtw.WriteElementString("name", member);

                        Serialize(xtw, pi.GetValue(targetObject, null), mappingAction, nestedObjs);

                        xtw.WriteEndElement();
                    }
                }

                xtw.WriteEndElement();
            }
            else if (xType == XmlRpcType.tVoid)
            {
                xtw.WriteElementString("string", "");
            }
            else
            {
                throw new XmlRpcUnsupportedTypeException(targetObject.GetType());
            }

            xtw.WriteEndElement();
        }

        void BuildArrayXml(XmlTextWriter xtw, Array ary, int curRank, int[] indices, MappingAction mappingAction, ArrayList nestedObjs)
        {
            xtw.WriteStartElement("", "array", "");
            xtw.WriteStartElement("", "data", "");

            if (curRank < (ary.Rank - 1))
            {
                for (int i = 0; i < ary.GetLength(curRank); i++)
                {
                    indices[curRank] = i;
                    xtw.WriteStartElement("", "value", "");
                    BuildArrayXml(xtw, ary, curRank + 1, indices, mappingAction, nestedObjs);
                    xtw.WriteEndElement();
                }
            }
            else
            {
                for (int i = 0; i < ary.GetLength(curRank); i++)
                {
                    indices[curRank] = i;
                    Serialize(xtw, ary.GetValue(indices), mappingAction, nestedObjs);
                }
            }
            xtw.WriteEndElement();
            xtw.WriteEndElement();
        }

        protected object ParseValue(XmlNode node, Type ValueType, ParseStack parseStack, MappingAction mappingAction)
        {
            return ParseValue(node, ValueType, parseStack, mappingAction, out _, out _);
        }

        object ParseValue(XmlNode node, Type ValueType, ParseStack parseStack, MappingAction mappingAction, out Type ParsedType, out Type ParsedArrayType)
        {
            var parser = new XmlParser();

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
                    retObj = ParseArray(node, valType, parseStack, mappingAction);
                else if (node.Name == "base64")
                    retObj = parser.ParseBase64(node, valType, parseStack);
                else if (node.Name == "struct")
                {
                    // if we don't know the expected class type then we must
                    // parse the XML-RPC class as an instance of XmlRpcStruct
                    if (valType != null && valType != typeof(XmlRpcStruct) && !valType.IsSubclassOf(typeof(XmlRpcStruct)))
                        retObj = ParseStruct(node, valType, parseStack, mappingAction);
                    else
                        retObj = ParseHashtable(node, parseStack, mappingAction);
                }
                else if (node.Name == "i4" || node.Name == "int") // integer has two representations in XML-RPC spec
                {
                    retObj = parser.ParseInt(node, valType, parseStack);
                    ParsedType = typeof(int);
                    ParsedArrayType = typeof(int[]);
                }
                else if (node.Name == "i8")
                {
                    retObj = parser.ParseLong(node, valType, parseStack);
                    ParsedType = typeof(long);
                    ParsedArrayType = typeof(long[]);
                }
                else if (node.Name == "string")
                {
                    retObj = parser.ParseString(node, valType, parseStack);
                    ParsedType = typeof(string);
                    ParsedArrayType = typeof(string[]);
                }
                else if (node.Name == "boolean")
                {
                    retObj = parser.ParseBoolean(node, valType, parseStack);
                    ParsedType = typeof(bool);
                    ParsedArrayType = typeof(bool[]);
                }
                else if (node.Name == "double")
                {
                    retObj = parser.ParseDouble(node, valType, parseStack);
                    ParsedType = typeof(double);
                    ParsedArrayType = typeof(double[]);
                }
                else if (node.Name == "dateTime.iso8601")
                {
                    retObj = parser.ParseDateTime(node, valType, parseStack, Configuration.MapEmptyDateTimeToMinValue(), Configuration.MapZerosDateTimeToMinValue());
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

        object ParseArray(
          XmlNode node,
          Type ValueType,
          ParseStack parseStack,
          MappingAction mappingAction)
        {
            // required type must be an array
            if (ValueType != null
              && !(ValueType.IsArray == true
                  || ValueType == typeof(Array)
                  || ValueType == typeof(object)))
            {
                throw new XmlRpcTypeMismatchException(parseStack.ParseType
                  + " contains array value where "
                  + XmlRpcServiceInfo.GetXmlRpcTypeString(ValueType)
                  + " expected " + parseStack.Dump());
            }
            if (ValueType != null)
            {
                XmlRpcType xmlRpcType = XmlRpcServiceInfo.GetXmlRpcType(ValueType);
                if (xmlRpcType == XmlRpcType.tMultiDimArray)
                {
                    parseStack.Push("array mapped to type " + ValueType.Name);
                    object ret = ParseMultiDimArray(node, ValueType, parseStack,
                      mappingAction);
                    return ret;
                }
                parseStack.Push("array mapped to type " + ValueType.Name);
            }
            else
                parseStack.Push("array");
            XmlNode dataNode = SelectSingleNode(node, "data");
            XmlNode[] childNodes = SelectNodes(dataNode, "value");
            int nodeCount = childNodes.Length;
            object[] elements = new object[nodeCount];
            // determine type of array elements
            Type elemType;
            if (ValueType != null
              && ValueType != typeof(Array)
              && ValueType != typeof(object))
            {

                elemType = ValueType.GetElementType();
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
                XmlNode vvNode = SelectValueNode(vNode);
                elements[i++] = ParseValue(vvNode, elemType, parseStack, mappingAction,
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
            if (ValueType != null
              && ValueType != typeof(Array)
              && ValueType != typeof(object))
            {
                retObj = CreateArrayInstance(ValueType, args);
            }
            else
            {
                if (useType == null)
                    retObj = CreateArrayInstance(typeof(object[]), args);
                else
                    retObj = CreateArrayInstance(useType, args);
            }
            for (int j = 0; j < elements.Length; j++)
            {
                ((Array)retObj).SetValue(elements[j], j);
            }
            parseStack.Pop();
            return retObj;
        }

        object ParseMultiDimArray(XmlNode node, Type ValueType,
          ParseStack parseStack, MappingAction mappingAction)
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
              parseStack, mappingAction);
            // build arguments to define array dimensions and create the array
            object[] args = new object[dimLengths.Length];
            for (int argi = 0; argi < dimLengths.Length; argi++)
            {
                args[argi] = dimLengths[argi];
            }
            Array ret = (Array)CreateArrayInstance(ValueType, args);
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
          ParseStack parseStack, MappingAction mappingAction)
        {
            if (node.Name != "array")
            {
                throw new XmlRpcTypeMismatchException(
                  "param element does not contain array element.");
            }
            XmlNode dataNode = SelectSingleNode(node, "data");
            XmlNode[] childNodes = SelectNodes(dataNode, "value");
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
                    XmlNode arrayNode = SelectSingleNode(vNode, "array");
                    ParseMultiDimElements(arrayNode, Rank, CurRank + 1, elemType,
                      elements, dimLengths, parseStack, mappingAction);
                }
            }
            else
            {
                foreach (XmlNode vNode in childNodes)
                {
                    XmlNode vvNode = SelectValueNode(vNode);
                    elements.Add(ParseValue(vvNode, elemType, parseStack,
                      mappingAction));
                }
            }
        }

        object ParseStruct(
          XmlNode node,
          Type valueType,
          ParseStack parseStack,
          MappingAction mappingAction)
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
            MappingAction localAction = mappingAction;
            if (valueType != null)
            {
                parseStack.Push("class mapped to type " + valueType.Name);
                localAction = StructMappingAction(valueType, mappingAction);
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
            XmlNode[] members = SelectNodes(node, "member");
            int fieldCount = 0;
            foreach (XmlNode member in members)
            {
                if (member.Name != "member")
                    continue;
                SelectTwoNodes(member, "name", out XmlNode nameNode, out bool dupName, "value",
                  out XmlNode valueNode, out bool dupValue);
                if (nameNode == null || nameNode.FirstChild == null)
                    throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType
                      + " contains a member with missing name"
                      + " " + parseStack.Dump());
                if (dupName)
                    throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType
                      + " contains member with more than one name element"
                      + " " + parseStack.Dump());
                string name = nameNode.FirstChild.Value;
                if (valueNode == null)
                    throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType
                      + " contains class member " + name + " with missing value "
                      + " " + parseStack.Dump());
                if (dupValue)
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
                    if (!Configuration.IgnoreDuplicateMembers())
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
                            XmlNode vvvNode = SelectValueNode(valueNode);
                            valObj = ParseValue(vvvNode, fi.FieldType,
                              parseStack, mappingAction);
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
                        XmlNode vvNode = SelectValueNode(valueNode);
                        valObj = ParseValue(vvNode, pi.PropertyType,
                          parseStack, mappingAction);
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

        void ReportMissingMembers(
          Type valueType,
          Hashtable names,
          ParseStack parseStack)
        {
            StringBuilder sb = new StringBuilder();
            int errorCount = 0;
            string sep = "";
            foreach (string s in names.Keys)
            {
                MappingAction memberAction = MemberMappingAction(valueType, s,
                  MappingAction.Error);
                if (memberAction == MappingAction.Error)
                {
                    sb.Append(sep);
                    sb.Append(s);
                    sep = " ";
                    errorCount++;
                }
            }
            if (errorCount > 0)
            {
                string plural = "";
                if (errorCount > 1)
                    plural = "s";
                throw new XmlRpcTypeMismatchException(parseStack.ParseType
                  + " contains class value with missing non-optional member"
                  + plural + ": " + sb.ToString() + " " + parseStack.Dump());
            }
        }

        string GetStructName(Type ValueType, string XmlRpcName)
        {
            // given a member name in an XML-RPC struct, check to see whether
            // a field has been associated with this XML-RPC member name, return
            // the field name if it has else return null
            if (ValueType == null)
                return null;
            foreach (FieldInfo fi in ValueType.GetFields())
            {
                Attribute attr = Attribute.GetCustomAttribute(fi,
                  typeof(XmlRpcMemberAttribute));
                if (attr != null
                  && attr is XmlRpcMemberAttribute
                  && ((XmlRpcMemberAttribute)attr).Member == XmlRpcName)
                {
                    string ret = fi.Name;
                    return ret;
                }
            }
            foreach (PropertyInfo pi in ValueType.GetProperties())
            {
                Attribute attr = Attribute.GetCustomAttribute(pi,
                  typeof(XmlRpcMemberAttribute));
                if (attr != null
                  && attr is XmlRpcMemberAttribute
                  && ((XmlRpcMemberAttribute)attr).Member == XmlRpcName)
                {
                    string ret = pi.Name;
                    return ret;
                }
            }
            return null;
        }

        MappingAction StructMappingAction(
          Type type,
          MappingAction currentAction)
        {
            // if class member has mapping action attribute, override the current
            // mapping action else just return the current action
            if (type == null)
                return currentAction;
            Attribute attr = Attribute.GetCustomAttribute(type,
              typeof(XmlRpcMissingMappingAttribute));
            if (attr != null)
                return ((XmlRpcMissingMappingAttribute)attr).Action;
            return currentAction;
        }

        MappingAction MemberMappingAction(
          Type type,
          string memberName,
          MappingAction currentAction)
        {
            // if class member has mapping action attribute, override the current
            // mapping action else just return the current action
            if (type == null)
                return currentAction;
            FieldInfo fi = type.GetField(memberName);
            Attribute attr;
            if (fi != null)
                attr = Attribute.GetCustomAttribute(fi,
                  typeof(XmlRpcMissingMappingAttribute));
            else
            {
                PropertyInfo pi = type.GetProperty(memberName);
                attr = Attribute.GetCustomAttribute(pi,
                  typeof(XmlRpcMissingMappingAttribute));
            }
            if (attr != null)
                return ((XmlRpcMissingMappingAttribute)attr).Action;
            return currentAction;
        }

        object ParseHashtable(
          XmlNode node,
            ParseStack parseStack,
            MappingAction mappingAction)
        {
            XmlRpcStruct retObj = new XmlRpcStruct();
            parseStack.Push("class mapped to XmlRpcStruct");
            try
            {
                XmlNode[] members = SelectNodes(node, "member");
                foreach (XmlNode member in members)
                {
                    if (member.Name != "member")
                        continue;
                    SelectTwoNodes(member, "name", out XmlNode nameNode, out bool dupName, "value",
                      out XmlNode valueNode, out bool dupValue);
                    if (nameNode == null || nameNode.FirstChild == null)
                        throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType
                          + " contains a member with missing name"
                          + " " + parseStack.Dump());
                    if (dupName)
                        throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType
                          + " contains member with more than one name element"
                          + " " + parseStack.Dump());
                    string rpcName = nameNode.FirstChild.Value;
                    if (valueNode == null)
                        throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType
                          + " contains class member " + rpcName + " with missing value "
                          + " " + parseStack.Dump());
                    if (dupValue)
                        throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType
                          + " contains member with more than one value element"
                          + " " + parseStack.Dump());
                    if (retObj.Contains(rpcName))
                    {
                        if (!Configuration.IgnoreDuplicateMembers())
                            throw new XmlRpcInvalidXmlRpcException(parseStack.ParseType
                              + " contains class value with duplicate member "
                              + nameNode.FirstChild.Value
                              + " " + parseStack.Dump());
                        else
                            continue;
                    }
                    object valObj;
                    parseStack.Push(String.Format("member {0}", rpcName));
                    try
                    {
                        XmlNode vvNode = SelectValueNode(valueNode);
                        valObj = ParseValue(vvNode, null, parseStack,
                          mappingAction);
                    }
                    finally
                    {
                        parseStack.Pop();
                    }
                    retObj.Add(rpcName, valObj);
                }
            }
            finally
            {
                parseStack.Pop();
            }
            return retObj;
        }



        public void SerializeFaultResponse(
          Stream stm,
          XmlRpcFaultException faultEx)
        {
            var fs = new FaultStruct
            {
                faultCode = faultEx.FaultCode,
                faultString = faultEx.FaultString
            };

            XmlTextWriter xtw = new XmlTextWriter(stm, Configuration.XmlEncoding);
            Configuration.ConfigureXmlFormat(xtw);
            xtw.WriteStartDocument();
            xtw.WriteStartElement("", "methodResponse", "");
            xtw.WriteStartElement("", "fault", "");
            Serialize(xtw, fs, MappingAction.Error);
            xtw.WriteEndElement();
            xtw.WriteEndElement();
            xtw.Flush();
        }

        protected XmlRpcFaultException ParseFault(XmlNode faultNode, ParseStack parseStack, MappingAction mappingAction)
        {
            XmlNode valueNode = SelectSingleNode(faultNode, "value");
            XmlNode structNode = SelectSingleNode(valueNode, "struct");
            if (structNode == null)
            {
                throw new XmlRpcInvalidXmlRpcException(
                  "class element missing from fault response.");
            }
            var fault = new XmlFault(); ;
            try
            {
                fault = (XmlFault)ParseValue(structNode, typeof(XmlFault), parseStack,
                  mappingAction);
            }
            catch (Exception ex)
            {
                // some servers incorrectly return fault code in a string
                if (Configuration.AllowStringFaultCode())
                    throw;
                else
                {
                    FaultStructStringCode faultStrCode;
                    try
                    {
                        faultStrCode = (FaultStructStringCode)ParseValue(structNode,
                          typeof(FaultStructStringCode), parseStack, mappingAction);
                        fault.faultCode = Convert.ToInt32(faultStrCode.faultCode);
                        fault.faultString = faultStrCode.faultString;
                    }
                    catch (Exception)
                    {
                        // use exception from when attempting to parse code as integer
                        throw ex;
                    }
                }
            }
            return new XmlRpcFaultException(fault.faultCode, fault.faultString);
        }

        protected XmlNode SelectSingleNode(XmlNode node, string name)
        {
            return node.SelectSingleNode(name);
        }

        protected XmlNode[] SelectNodes(XmlNode node, string name)
        {
            ArrayList list = new ArrayList();
            foreach (XmlNode selnode in node.ChildNodes)
            {
                if (selnode.Name == name)
                    list.Add(selnode);
            }
            return (XmlNode[])list.ToArray(typeof(XmlNode));
        }

        protected XmlNode SelectValueNode(XmlNode valueNode)
        {
            // an XML-RPC value is either held as the child node of a <value> element
            // or is just the text of the value node as an implicit string value
            XmlNode vvNode = SelectSingleNode(valueNode, "*");
            if (vvNode == null)
                vvNode = valueNode.FirstChild;
            return vvNode;
        }

        void SelectTwoNodes(XmlNode node, string name1, out XmlNode node1,
          out bool dup1, string name2, out XmlNode node2, out bool dup2)
        {
            node1 = node2 = null;
            dup1 = dup2 = false;
            foreach (XmlNode selnode in node.ChildNodes)
            {
                if (selnode.Name == name1)
                {
                    if (node1 == null)
                        node1 = selnode;
                    else
                        dup1 = true;
                }
                else if (selnode.Name == name2)
                {
                    if (node2 == null)
                        node2 = selnode;
                    else
                        dup2 = true;
                }
            }
        }

        // TODO: following to return Array?
        protected object CreateArrayInstance(Type type, object[] args)
        {
            return Activator.CreateInstance(type, args);
        }
    }
}
