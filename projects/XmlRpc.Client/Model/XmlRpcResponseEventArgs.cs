using System;
using System.IO;

namespace XmlRpc.Client.Model
{
    public class XmlRpcResponseEventArgs : EventArgs
    {
        public Guid ProxyID { get; }
        public long RequestNum { get; }
        public Stream ResponseStream { get; }

        public XmlRpcResponseEventArgs(Guid guid, long request, Stream responseStream)
        {
            ProxyID = guid;
            RequestNum = request;
            ResponseStream = responseStream;
        }
    }
}

