using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using XmlRpc.Client.Attributes;
using XmlRpc.Client.Exceptions;
using XmlRpc.Client.Model;
using XmlRpc.Client.Serializer.Model;

namespace XmlRpc.Client.Serializer.Request
{
    public class XmlRpcRequestSerializer : XmlRpcSerializer
    {
        public XmlRpcRequestSerializer(SerializerConfig serializerConfig) : base(serializerConfig) { }

        public void SerializeRequest(Stream outputStream, XmlRpcRequest request)
        {
            var xtw = new XmlTextWriter(outputStream, Configuration.XmlEncoding);
            Configuration.ConfigureXmlFormat(xtw);

            xtw.WriteStartDocument();
            xtw.WriteStartElement("", "methodCall", "");
            xtw.WriteElementString("methodName", request.Method);

            if (request.Arguments.Length > 0 || Configuration.UseEmptyParamsTag)
                WriteArguments(request, xtw);

            xtw.WriteEndElement();
            xtw.Flush();
        }

        void WriteArguments(XmlRpcRequest request, XmlTextWriter xtw)
        {
            try
            {
                xtw.WriteStartElement("", "params", "");

                if (!IsStructParamsMethod(request.MethodInfo))
                    SerializeParams(xtw, request);
                else
                    SerializeStructParams(xtw, request);

                xtw.WriteEndElement();
            }
            catch (XmlRpcUnsupportedTypeException ex)
            {
                throw new XmlRpcUnsupportedTypeException(ex.UnsupportedType, $"A parameter is of, or contains an instance of, type {ex.UnsupportedType} which cannot be mapped to an XML-RPC type");
            }
        }

        void SerializeParams(XmlTextWriter xtw, XmlRpcRequest request)
        {
            var parameterInfos = request.MethodInfo?.GetParameters();

            if (parameterInfos != null && parameterInfos.Length != request.Arguments.Length)
                throw new XmlRpcInvalidParametersException("Number of request parameters does not match number of proxy method parameters.");

            if (request.Arguments.Any(a => a == null))
                throw new XmlRpcNullParameterException($"Null method parameter not allowed.");

            for (int i = 0; i < request.Arguments.Length; i++)
            {
                if (parameterInfos != null && Attribute.IsDefined(parameterInfos[i], typeof(ParamArrayAttribute)))
                {
                    var arry = (Array)request.Arguments[i];
                    WriteParamsParameter(arry, xtw);
                    break;
                }

                xtw.WriteStartElement("", "param", "");
                Serialize(xtw, request.Arguments[i]);
                xtw.WriteEndElement();
            }
        }

        void WriteParamsParameter(Array paramArray, XmlTextWriter xtw)
        {
            foreach (var param in paramArray)
            {
                if (param == null)
                    throw new XmlRpcNullParameterException("Null parameter in params array");

                xtw.WriteStartElement("", "param", "");
                Serialize(xtw, param);
                xtw.WriteEndElement();
            }
        }

        void SerializeStructParams(XmlTextWriter xtw, XmlRpcRequest request)
        {
            var parameterInfos = request.MethodInfo.GetParameters();

            if (request.Arguments.Any(a => a == null))
                throw new XmlRpcNullParameterException($"Null method parameter not allowed.");

            if (request.Arguments.Length != parameterInfos.Length)
                throw new XmlRpcInvalidParametersException("Number of request parameters does not match number of proxy method parameters.");

            if (Attribute.IsDefined(parameterInfos[request.Arguments.Length - 1], typeof(ParamArrayAttribute)))
                throw new XmlRpcInvalidParametersException("params parameter cannot be used with StructParams.");

            xtw.WriteStartElement("", "param", "");
            xtw.WriteStartElement("", "value", "");
            xtw.WriteStartElement("", "struct", "");

            for (int i = 0; i < request.Arguments.Length; i++)
            {
                xtw.WriteStartElement("", "member", "");
                xtw.WriteElementString("name", parameterInfos[i].Name);

                Serialize(xtw, request.Arguments[i]);

                xtw.WriteEndElement();
            }

            xtw.WriteEndElement();
            xtw.WriteEndElement();
            xtw.WriteEndElement();
        }

        bool IsStructParamsMethod(MethodInfo methodInfo)
        {
            if (methodInfo == null)
                return false;

            var ret = false;
            var attr = Attribute.GetCustomAttribute(methodInfo, typeof(XmlRpcMethodAttribute));
            if (attr != null)
            {
                var mattr = (XmlRpcMethodAttribute)attr;
                ret = mattr.StructParams;
            }

            return ret;
        }
    }
}
