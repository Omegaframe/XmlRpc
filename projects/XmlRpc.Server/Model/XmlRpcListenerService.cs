using System;
using System.Net;
using XmlRpc.Server.Protocol;

namespace XmlRpc.Server.Model
{
    public abstract class XmlRpcListenerService : XmlRpcHttpServerProtocol
    {
        public virtual void ProcessRequest(HttpListenerContext RequestContext)
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