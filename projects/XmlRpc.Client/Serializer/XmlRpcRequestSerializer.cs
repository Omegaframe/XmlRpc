using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using XmlRpc.Client.Attributes;
using XmlRpc.Client.Exceptions;
using XmlRpc.Client.Model;

namespace XmlRpc.Client.Serializer
{
    public class XmlRpcRequestSerializer : XmlRpcSerializer
    {
        public void SerializeRequest(Stream stm, XmlRpcRequest request)
        {
            var xtw = new XmlTextWriter(stm, Configuration.XmlEncoding);
            Configuration.ConfigureXmlFormat(xtw);

            xtw.WriteStartDocument();
            xtw.WriteStartElement("", "methodCall", "");
            xtw.WriteElementString("methodName", request.xmlRpcMethod ?? request.method);

            if (request.args.Length > 0 || Configuration.UseEmptyParamsTag)
                WriteArguments(request, xtw);

            xtw.WriteEndElement();
            xtw.Flush();
        }

        void WriteArguments(XmlRpcRequest request, XmlTextWriter xtw)
        {
            try
            {
                xtw.WriteStartElement("", "params", "");

                if (!IsStructParamsMethod(request.mi))
                    SerializeParams(xtw, request, Configuration.MappingAction);
                else
                    SerializeStructParams(xtw, request, Configuration.MappingAction);

                xtw.WriteEndElement();
            }
            catch (XmlRpcUnsupportedTypeException ex)
            {
                throw new XmlRpcUnsupportedTypeException(ex.UnsupportedType, $"A parameter is of, or contains an instance of, type {ex.UnsupportedType} which cannot be mapped to an XML-RPC type");
            }
        }

        void SerializeParams(XmlTextWriter xtw, XmlRpcRequest request, MappingAction mappingAction)
        {
            var pis = request.mi?.GetParameters();

            if (pis != null && pis.Length != request.args.Length)
                throw new XmlRpcInvalidParametersException("Number of request parameters does not match number of proxy method parameters.");

            if (request.args.Any(a => a == null))
                throw new XmlRpcNullParameterException($"Null method parameter not allowed.");

            for (int i = 0; i < request.args.Length; i++)
            {
                if (pis != null && Attribute.IsDefined(pis[i], typeof(ParamArrayAttribute)))
                {
                    var arry = (Array)request.args[i];
                    WriteParamsParameter(arry, mappingAction, xtw);
                    break;
                }

                xtw.WriteStartElement("", "param", "");
                Serialize(xtw, request.args[i], mappingAction);
                xtw.WriteEndElement();
            }
        }

        void WriteParamsParameter(Array paramArray, MappingAction mappingAction, XmlTextWriter xtw)
        {
            foreach (var param in paramArray)
            {
                if (param == null)
                    throw new XmlRpcNullParameterException("Null parameter in params array");

                xtw.WriteStartElement("", "param", "");
                Serialize(xtw, param, mappingAction);
                xtw.WriteEndElement();
            }
        }

        void SerializeStructParams(XmlTextWriter xtw, XmlRpcRequest request, MappingAction mappingAction)
        {
            var pis = request.mi.GetParameters();

            if (request.args.Any(a => a == null))
                throw new XmlRpcNullParameterException($"Null method parameter not allowed.");

            if (request.args.Length != pis.Length)
                throw new XmlRpcInvalidParametersException("Number of request parameters does not match number of proxy method parameters.");

            if (Attribute.IsDefined(pis[request.args.Length - 1], typeof(ParamArrayAttribute)))
                throw new XmlRpcInvalidParametersException("params parameter cannot be used with StructParams.");

            xtw.WriteStartElement("", "param", "");
            xtw.WriteStartElement("", "value", "");
            xtw.WriteStartElement("", "struct", "");

            for (int i = 0; i < request.args.Length; i++)
            {
                xtw.WriteStartElement("", "member", "");
                xtw.WriteElementString("name", pis[i].Name);

                Serialize(xtw, request.args[i], mappingAction);

                xtw.WriteEndElement();
            }

            xtw.WriteEndElement();
            xtw.WriteEndElement();
            xtw.WriteEndElement();
        }

        bool IsStructParamsMethod(MethodInfo mi)
        {
            if (mi == null)
                return false;

            var ret = false;
            var attr = Attribute.GetCustomAttribute(mi, typeof(XmlRpcMethodAttribute));
            if (attr != null)
            {
                var mattr = (XmlRpcMethodAttribute)attr;
                ret = mattr.StructParams;
            }

            return ret;
        }
    }
}
