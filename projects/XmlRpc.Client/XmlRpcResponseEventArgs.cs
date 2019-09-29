using System;
using System.IO;

namespace XmlRpc.Client
{
    public class XmlRpcResponseEventArgs : EventArgs
    {
        Guid _guid;
        long _request;
        Stream _responseStream;

        public XmlRpcResponseEventArgs(Guid guid, long request,
          Stream responseStream)
        {
            _guid = guid;
            _request = request;
            _responseStream = responseStream;
        }

        public Guid ProxyID
        {
            get { return _guid; }
        }

        public long RequestNum
        {
            get { return _request; }
        }

        public Stream ResponseStream
        {
            get { return _responseStream; }
        }
    }
}
