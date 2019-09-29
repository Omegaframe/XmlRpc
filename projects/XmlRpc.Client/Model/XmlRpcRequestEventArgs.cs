using System;
using System.IO;

namespace XmlRpc.Client.Model
{
    public class XmlRpcRequestEventArgs : EventArgs
    {
        public Guid ProxyID { get; }
        public long RequestNum { get; }
        public Stream RequestStream { get; }

        public XmlRpcRequestEventArgs(Guid guid, long request, Stream requestStream)
        {
            ProxyID = guid;
            RequestNum = request;
            RequestStream = requestStream;
        }
    }
}
