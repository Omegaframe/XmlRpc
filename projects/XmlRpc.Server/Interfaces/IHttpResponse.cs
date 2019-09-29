using System.IO;
using System.Net;

namespace XmlRpc.Server.Interfaces
{
    public interface IHttpResponse
    {
        long ContentLength { set; }
        string ContentType { get; set; }
        TextWriter Output { get; }
        Stream OutputStream { get; }
        int StatusCode { get; set; }
        string StatusDescription { get; set; }
        WebHeaderCollection AdditionalHeaders { get; }
    }
}
