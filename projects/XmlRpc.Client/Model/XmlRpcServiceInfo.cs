using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using XmlRpc.Client.Attributes;
using XmlRpc.Client.DataTypes;
using XmlRpc.Client.Exceptions;

namespace XmlRpc.Client.Model
{
    public class XmlRpcServiceInfo
    {
        public static XmlRpcServiceInfo CreateServiceInfo(Type type)
        {
            var svcInfo = new XmlRpcServiceInfo();
            var svcAttr = (XmlRpcServiceAttribute)Attribute.GetCustomAttribute(type, typeof(XmlRpcServiceAttribute));

            if (!string.IsNullOrWhiteSpace(svcAttr?.Description))
                svcInfo.Doc = svcAttr.Description;

            if (!string.IsNullOrWhiteSpace(svcAttr?.Name))
                svcInfo.Name = svcAttr.Name;
            else
                svcInfo.Name = type.Name;

            var methods = new Hashtable();

            foreach (var itf in type.GetInterfaces())
            {
                var itfAttr = (XmlRpcServiceAttribute)Attribute.GetCustomAttribute(itf, typeof(XmlRpcServiceAttribute));
                if (itfAttr != null)
                    svcInfo.Doc = itfAttr.Description;

                var imap = type.GetInterfaceMap(itf);
                foreach (var mi in imap.InterfaceMethods)
                    ExtractMethodInfo(methods, mi, itf);
            }

            foreach (var mi in type.GetMethods())
            {
                var mthds = new ArrayList();
                mthds.Add(mi);

                var curMi = mi;
                while (true)
                {
                    var baseMi = curMi.GetBaseDefinition();
                    if (baseMi.DeclaringType == curMi.DeclaringType)
                        break;

                    mthds.Insert(0, baseMi);
                    curMi = baseMi;
                }

                foreach (MethodInfo mthd in mthds)
                    ExtractMethodInfo(methods, mthd, type);
            }

            svcInfo.Methods = new XmlRpcMethodInfo[methods.Count];
            methods.Values.CopyTo(svcInfo.Methods, 0);
            Array.Sort(svcInfo.Methods);

            return svcInfo;
        }

        static void ExtractMethodInfo(Hashtable methods, MethodInfo mi, Type type)
        {
            var attr = (XmlRpcMethodAttribute)Attribute.GetCustomAttribute(mi, typeof(XmlRpcMethodAttribute));
            if (attr == null)
                return;

            var mthdInfo = new XmlRpcMethodInfo();
            mthdInfo.MethodInfo = mi;
            mthdInfo.XmlRpcName = GetXmlRpcMethodName(mi);
            mthdInfo.MiName = mi.Name;
            mthdInfo.Doc = attr.Description;
            mthdInfo.IsHidden = attr.IntrospectionMethod | attr.Hidden;

            var parmList = new ArrayList();
            var parms = mi.GetParameters();
            foreach (var parm in parms)
            {
                var parmInfo = new XmlRpcParameterInfo();
                parmInfo.Name = parm.Name;
                parmInfo.Type = parm.ParameterType;
                parmInfo.XmlRpcType = GetXmlRpcTypeString(parm.ParameterType);

                parmInfo.Doc = "";
                var pattr = (XmlRpcParameterAttribute)Attribute.GetCustomAttribute(parm, typeof(XmlRpcParameterAttribute));
                if (pattr != null)
                {
                    parmInfo.Doc = pattr.Description;
                    parmInfo.XmlRpcName = pattr.Name;
                }

                parmInfo.IsParams = Attribute.IsDefined(parm, typeof(ParamArrayAttribute));
                parmList.Add(parmInfo);
            }

            mthdInfo.Parameters = (XmlRpcParameterInfo[])parmList.ToArray(typeof(XmlRpcParameterInfo));
            mthdInfo.ReturnType = mi.ReturnType;
            mthdInfo.ReturnXmlRpcType = GetXmlRpcTypeString(mi.ReturnType);

            var orattrs = mi.ReturnTypeCustomAttributes.GetCustomAttributes(typeof(XmlRpcReturnValueAttribute), false);
            if (orattrs.Length > 0)
                mthdInfo.ReturnDoc = ((XmlRpcReturnValueAttribute)orattrs[0]).Description;

            if (methods[mthdInfo.XmlRpcName] != null)
                throw new XmlRpcDupXmlRpcMethodNames($"Method {mi.Name} in type {type.Name} has duplicate XmlRpc method name {mthdInfo.XmlRpcName}");

            methods.Add(mthdInfo.XmlRpcName, mthdInfo);
        }

        public MethodInfo[] GetMethodInfos(string xmlRpcMethodName)
        {
            var methods = Methods.Where(m => m.XmlRpcName.Equals(xmlRpcMethodName, StringComparison.OrdinalIgnoreCase))
                .Select(m => m.MethodInfo);
            return methods.ToArray();
        }

        public static string GetXmlRpcMethodName(MethodInfo mi)
        {
            var attr = (XmlRpcMethodAttribute)Attribute.GetCustomAttribute(mi, typeof(XmlRpcMethodAttribute));
            if (!string.IsNullOrWhiteSpace(attr?.Method))
                return attr.Method;

            return mi.Name;
        }

        public string GetMethodName(string XmlRpcMethodName)
        {
            foreach (XmlRpcMethodInfo methodInfo in Methods)
                if (methodInfo.XmlRpcName == XmlRpcMethodName)
                    return methodInfo.MiName;

            return null;
        }

        public string Doc { get; set; }

        public string Name { get; set; }

        public XmlRpcMethodInfo[] Methods { get; private set; }

        public XmlRpcMethodInfo[] GetMethods(string methodName)
        {
            var infos = Methods.Where(info => info.XmlRpcName.Equals(methodName, StringComparison.OrdinalIgnoreCase));
            return infos.Distinct().ToArray();
        }

        XmlRpcServiceInfo()
        {
        }

        public static XmlRpcType GetXmlRpcType(Type t)
        {
            return GetXmlRpcType(t, new Stack());
        }

        static XmlRpcType GetXmlRpcType(Type t, Stack typeStack)
        {
            XmlRpcType ret;
            if (t == typeof(int))
                ret = XmlRpcType.Int32;
            else if (t == typeof(XmlRpcInt))
                ret = XmlRpcType.Int32;
            else if (t == typeof(long))
                ret = XmlRpcType.Int64;
            else if (t == typeof(bool))
                ret = XmlRpcType.Boolean;
            else if (t == typeof(XmlRpcBoolean))
                ret = XmlRpcType.Boolean;
            else if (t == typeof(string))
                ret = XmlRpcType.String;
            else if (t == typeof(double))
                ret = XmlRpcType.Double;
            else if (t == typeof(XmlRpcDouble))
                ret = XmlRpcType.Double;
            else if (t == typeof(DateTime))
                ret = XmlRpcType.DateTime;
            else if (t == typeof(XmlRpcDateTime))
                ret = XmlRpcType.DateTime;
            else if (t == typeof(byte[]))
                ret = XmlRpcType.Base64;
            else if (t == typeof(XmlRpcStruct))
                ret = XmlRpcType.Hashtable;
            else if (t == typeof(Array))
                ret = XmlRpcType.Array;
            else if (t.IsArray)
            {

                var elemType = t.GetElementType();
                if (elemType != typeof(object) && GetXmlRpcType(elemType, typeStack) == XmlRpcType.Invalid)
                {
                    ret = XmlRpcType.Invalid;
                }
                else
                {
                    if (t.GetArrayRank() == 1)  // single dim array
                        ret = XmlRpcType.Array;
                    else
                        ret = XmlRpcType.MultiDimArray;
                }
            }

            else if (t == typeof(int?))
                ret = XmlRpcType.Int32;
            else if (t == typeof(long?))
                ret = XmlRpcType.Int64;
            else if (t == typeof(bool?))
                ret = XmlRpcType.Boolean;
            else if (t == typeof(double?))
                ret = XmlRpcType.Double;
            else if (t == typeof(DateTime?))
                ret = XmlRpcType.DateTime;
            else if (t == typeof(void))
                ret = XmlRpcType.Void;
            else if ((t.IsValueType && !t.IsPrimitive && !t.IsEnum) || t.IsClass)
            {
                // if type is class or class its only valid for XML-RPC mapping if all 
                // its members have a valid mapping or are of type object which
                // maps to any XML-RPC type
                var mis = t.GetMembers();
                foreach (var mi in mis)
                {
                    if (mi.MemberType == MemberTypes.Field)
                    {
                        var fi = (FieldInfo)mi;
                        if (typeStack.Contains(fi.FieldType))
                            continue;
                        try
                        {
                            typeStack.Push(fi.FieldType);
                            if ((fi.FieldType != typeof(object) && GetXmlRpcType(fi.FieldType, typeStack) == XmlRpcType.Invalid))
                                return XmlRpcType.Invalid;
                        }
                        finally
                        {
                            typeStack.Pop();
                        }
                    }
                    else if (mi.MemberType == MemberTypes.Property)
                    {
                        var pi = (PropertyInfo)mi;
                        if (typeStack.Contains(pi.PropertyType))
                            continue;

                        try
                        {
                            typeStack.Push(pi.PropertyType);
                            if ((pi.PropertyType != typeof(object) && GetXmlRpcType(pi.PropertyType, typeStack) == XmlRpcType.Invalid))
                                return XmlRpcType.Invalid;
                        }
                        finally
                        {
                            typeStack.Pop();
                        }
                    }
                }
                ret = XmlRpcType.Struct;
            }
            else
                ret = XmlRpcType.Invalid;
            return ret;
        }

        static public string GetXmlRpcTypeString(Type t)
        {
            var rpcType = GetXmlRpcType(t);
            return GetXmlRpcTypeString(rpcType);
        }

        static public string GetXmlRpcTypeString(XmlRpcType t)
        {
            string ret;
            if (t == XmlRpcType.Int32)
                ret = "integer";
            else if (t == XmlRpcType.Int64)
                ret = "i8";
            else if (t == XmlRpcType.Boolean)
                ret = "boolean";
            else if (t == XmlRpcType.String)
                ret = "string";
            else if (t == XmlRpcType.Double)
                ret = "double";
            else if (t == XmlRpcType.DateTime)
                ret = "dateTime";
            else if (t == XmlRpcType.Base64)
                ret = "base64";
            else if (t == XmlRpcType.Struct)
                ret = "struct";
            else if (t == XmlRpcType.Hashtable)
                ret = "struct";
            else if (t == XmlRpcType.Array)
                ret = "array";
            else if (t == XmlRpcType.MultiDimArray)
                ret = "array";
            else if (t == XmlRpcType.Void)
                ret = "void";
            else
                ret = null;
            return ret;
        }
    }
}