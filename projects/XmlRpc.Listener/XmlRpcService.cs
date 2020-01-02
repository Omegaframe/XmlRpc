using System;
using System.Net;
using XmlRpc.Listener.Internal;
using XmlRpc.Server.Protocol;

namespace XmlRpc.Listener
{
    public abstract class XmlRpcService : XmlRpcHttpServerProtocol
    {
        public void ProcessRequest(HttpListenerContext RequestContext)
        {
            try
            {
                var req = new XmlRpcListenerRequest(RequestContext.Request);
                var resp = new XmlRpcListenerResponse(RequestContext.Response);
                HandleHttpRequest(req, resp);
            }
            catch (Exception ex)
            {
                RequestContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                RequestContext.Response.StatusDescription = ex.Message;
            }
        }
    }
}