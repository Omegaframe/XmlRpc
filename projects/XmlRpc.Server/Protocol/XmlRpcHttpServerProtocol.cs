using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Xml;
using XmlRpc.Client.Attributes;
using XmlRpc.Server.Interfaces;

namespace XmlRpc.Server.Protocol
{
    public class XmlRpcHttpServerProtocol : XmlRpcServerProtocol, IHttpRequestHandler
    {
        public void HandleHttpRequest(IHttpRequest httpReq, IHttpResponse httpResp)
        {
            if (httpReq.HttpMethod.Equals(HttpMethod.Get.Method, StringComparison.OrdinalIgnoreCase))
            {
                HandleGet(httpResp);
                return;
            }

            if (!httpReq.HttpMethod.Equals(HttpMethod.Post.Method, StringComparison.OrdinalIgnoreCase))
            {
                HandleUnsupportedMethod(httpResp);
                return;
            }

            var responseStream = Invoke(httpReq.InputStream);

            httpResp.StatusCode = (int)HttpStatusCode.OK;
            httpResp.ContentType = MediaTypeNames.Text.Xml;
            httpResp.ContentLength = responseStream.Length;

            responseStream.CopyTo(httpResp.OutputStream);
        }

        void HandleGet(IHttpResponse httpResp)
        {
            var svcAttr = (XmlRpcServiceAttribute)Attribute.GetCustomAttribute(GetType(), typeof(XmlRpcServiceAttribute));
            if (svcAttr?.AutoDocumentation == false)
            {
                HandleUnsupportedMethod(httpResp);
                return;
            }

            using (var stm = new MemoryStream())
            using (var streamWriter = new StreamWriter(stm))
            using (var wrtr = new XmlTextWriter(streamWriter))
            {
                var autoDocVersion = svcAttr?.AutoDocVersion ?? true;
                XmlRpcDocWriter.WriteDoc(wrtr, GetType(), autoDocVersion);
                wrtr.Flush();

                httpResp.StatusCode = (int)HttpStatusCode.OK;
                httpResp.ContentType = MediaTypeNames.Text.Html;
                httpResp.ContentLength = stm.Length;

                stm.Seek(0, SeekOrigin.Begin);
                stm.CopyTo(httpResp.OutputStream);
            }
        }

        void HandleUnsupportedMethod(IHttpResponse httpResp)
        {
            httpResp.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            httpResp.AdditionalHeaders.Add(HttpResponseHeader.Allow, HttpMethod.Get.Method);
            httpResp.AdditionalHeaders.Add(HttpResponseHeader.Allow, HttpMethod.Post.Method);
            httpResp.StatusDescription = "Unsupported HTTP Method";
        }
    }
}
