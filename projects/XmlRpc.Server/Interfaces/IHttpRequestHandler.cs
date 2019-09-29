namespace XmlRpc.Server.Interfaces
{
    public interface IHttpRequestHandler
    {
        void HandleHttpRequest(IHttpRequest httpReq, IHttpResponse httpResp);
    }
}
