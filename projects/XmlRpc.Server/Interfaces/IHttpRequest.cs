using System.IO;

namespace XmlRpc.Server.Interfaces
{
    public interface IHttpRequest
    {
        Stream InputStream { get; }
        string HttpMethod { get; }
    }
}