using System;
using System.IO;
using System.Xml;
using XmlRpc.Client.Attributes;
using XmlRpc.Server.Interfaces;

namespace XmlRpc.Server.Protocol
{
    public class XmlRpcHttpServerProtocol : XmlRpcServerProtocol,
      IHttpRequestHandler
    {
        public void HandleHttpRequest(
          IHttpRequest httpReq,
          IHttpResponse httpResp)
        {
            // GET has its own handler because it can be used to return a 
            // HTML description of the service
            if (httpReq.HttpMethod == "GET")
            {
                XmlRpcServiceAttribute svcAttr = (XmlRpcServiceAttribute)
                  Attribute.GetCustomAttribute(GetType(), typeof(XmlRpcServiceAttribute));
                if (svcAttr != null && svcAttr.AutoDocumentation == false)
                {
                    HandleUnsupportedMethod(httpResp);
                }
                else
                {
                    bool autoDocVersion = true;
                    if (svcAttr != null)
                        autoDocVersion = svcAttr.AutoDocVersion;
                    HandleGET(httpResp, autoDocVersion);
                }
                return;
            }
            // calls on service methods are via POST
            if (httpReq.HttpMethod != "POST")
            {
                HandleUnsupportedMethod(httpResp);
                return;
            }
            //Context.Response.AppendHeader("Server", "XML-RPC.NET");
            // process the request
            Stream responseStream = Invoke(httpReq.InputStream);
            httpResp.ContentType = "text/xml";
            httpResp.ContentLength = responseStream.Length;

            Stream respStm = httpResp.OutputStream;
            responseStream.CopyTo(respStm);
            respStm.Flush();
        }

        protected void HandleGET(
          IHttpResponse httpResp,
            bool autoDocVersion)
        {
            using (MemoryStream stm = new MemoryStream())
            {
                using (var wrtr = new XmlTextWriter(new StreamWriter(stm)))
                {
                    XmlRpcDocWriter.WriteDoc(wrtr, this.GetType(), autoDocVersion);
                    wrtr.Flush();
                    httpResp.ContentType = "text/html";
                    httpResp.ContentLength = stm.Length;

                    stm.Position = 0;
                    Stream respStm = httpResp.OutputStream;
                    stm.CopyTo(respStm);
                    respStm.Flush();
                    httpResp.StatusCode = 200;
                }
            }
        }

        protected void HandleUnsupportedMethod(
          IHttpResponse httpResp)
        {
            // RFC 2068 error 405: "The method specified in the Request-Line   
            // is not allowed for the resource identified by the Request-URI. 
            // The response MUST include an Allow header containing a list 
            // of valid methods for the requested resource."
            //!! add Allow header
            httpResp.StatusCode = 405;
            httpResp.StatusDescription = "Unsupported HTTP verb";
        }

    }
}
