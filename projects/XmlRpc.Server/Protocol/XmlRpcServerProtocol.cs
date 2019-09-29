using System;
using System.IO;
using System.Text;
using XmlRpc.Client;
using XmlRpc.Client.Attributes;
using XmlRpc.Client.Exceptions;
using XmlRpc.Client.Model;

namespace XmlRpc.Server.Protocol
{
    public class XmlRpcServerProtocol : SystemMethodsBase
    {
        public Stream Invoke(Stream requestStream)
        {
            try
            {
                return TryInvoke(requestStream);
            }
            catch (Exception ex)
            {
                return CreateExceptionResponse(ex);
            }
        }

        Stream TryInvoke(Stream requestStream)
        {
            var serializer = new XmlRpcSerializer();
            var serviceAttr = (XmlRpcServiceAttribute)Attribute.GetCustomAttribute(GetType(), typeof(XmlRpcServiceAttribute));

            if (serviceAttr != null)
                SetSerializerSettings(serviceAttr, serializer);

            var xmlRpcReq = serializer.DeserializeRequest(requestStream, GetType());
            var xmlRpcResp = Invoke(xmlRpcReq);

            var responseStream = new MemoryStream();
            serializer.SerializeResponse(responseStream, xmlRpcResp);
            responseStream.Seek(0, SeekOrigin.Begin);
            return responseStream;
        }

        void SetSerializerSettings(XmlRpcServiceAttribute serviceAttr, XmlRpcSerializer serializer)
        {
            if (serviceAttr.XmlEncoding != null)
                serializer.XmlEncoding = Encoding.GetEncoding(serviceAttr.XmlEncoding);

            serializer.UseIntTag = serviceAttr.UseIntTag;
            serializer.UseStringTag = serviceAttr.UseStringTag;
            serializer.UseIndentation = serviceAttr.UseIndentation;
            serializer.Indentation = serviceAttr.Indentation;
        }

        Stream CreateExceptionResponse(Exception exception)
        {
            XmlRpcFaultException fex;
            if (exception is XmlRpcException)
                fex = new XmlRpcFaultException(0, ((XmlRpcException)exception).Message);
            else if (exception is XmlRpcFaultException)
                fex = (XmlRpcFaultException)exception;
            else
                fex = new XmlRpcFaultException(0, exception.Message);

            var serializer = new XmlRpcSerializer();
            var responseStream = new MemoryStream();
            serializer.SerializeFaultResponse(responseStream, fex);
            responseStream.Seek(0, SeekOrigin.Begin);
            return responseStream;
        }

        XmlRpcResponse Invoke(XmlRpcRequest request)
        {
            var mi = request.mi ?? GetType().GetMethod(request.method);

            try
            {
                var reto = mi.Invoke(this, request.args);

                var response = new XmlRpcResponse(reto);
                return response;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    throw ex.InnerException;

                throw ex;
            }
        }
    }
}
