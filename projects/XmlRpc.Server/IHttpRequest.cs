using System.IO;

namespace XmlRpc.Server
{
    public interface IHttpRequest
    {
        Stream InputStream { get; }
        string HttpMethod { get; }
    }
}