using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
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
            catch (NullReferenceException)
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
            var parser = new XmlParser(Configuration);

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
                var structAction = parser.StructMappingAction(targetObject.GetType(), mappingAction);

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
                            var memberAction = parser.MemberMappingAction(targetObject.GetType(), fi.Name, structAction);
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
                            var memberAction = parser.MemberMappingAction(targetObject.GetType(), pi.Name, structAction);
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

        public void SerializeFaultResponse(Stream stm, XmlRpcFaultException faultEx)
        {
            var fs = new FaultStruct
            {
                faultCode = faultEx.FaultCode,
                faultString = faultEx.FaultString
            };

            var xtw = new XmlTextWriter(stm, Configuration.XmlEncoding);
            Configuration.ConfigureXmlFormat(xtw);

            xtw.WriteStartDocument();
            xtw.WriteStartElement("", "methodResponse", "");
            xtw.WriteStartElement("", "fault", "");

            Serialize(xtw, fs, MappingAction.Error);

            xtw.WriteEndElement();
            xtw.WriteEndElement();
            xtw.Flush();
        }
    }
}
