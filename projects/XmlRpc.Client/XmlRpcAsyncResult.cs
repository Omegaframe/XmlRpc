using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using XmlRpc.Client.Model;


namespace XmlRpc.Client
{
    public class XmlRpcAsyncResult : IAsyncResult
    {
        public object AsyncState { get; }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (manualResetEvent == null)
                {
                    lock (this)
                        manualResetEvent = new ManualResetEvent(IsCompleted);
                }

                return manualResetEvent;
            }
        }

        public bool CompletedSynchronously
        {
            get { return completedSynchronously; }
            set
            {
                if (completedSynchronously)
                    completedSynchronously = value;
            }
        }

        public bool IsCompleted { get; private set; }

        public CookieCollection ResponseCookies => _responseCookies;

        public WebHeaderCollection ResponseHeaders => _responseHeaders;

        public bool UseEmptyParamsTag { get; }

        public bool UseIndentation { get; }

        public int Indentation { get; }

        public bool UseIntTag { get; }

        public bool UseStringTag { get; }

        public void Abort() => Request?.Abort();

        public Exception Exception { get; private set; }

        public XmlRpcClientProtocol ClientProtocol { get; }

        internal XmlRpcAsyncResult(
          XmlRpcClientProtocol clientProtocol,
            XmlRpcRequest rpcRequest,
            Encoding xmlEncoding,
            bool useEmptyParamsTag,
            bool useIndentation,
            int indentation,
            bool useIntTag,
            bool useStringTag,
            WebRequest request,
            AsyncCallback userCallback,
            object userAsyncState)
        {
            XmlRpcRequest = rpcRequest;
            ClientProtocol = clientProtocol;
            Request = request;
            AsyncState = userAsyncState;
            this.userCallback = userCallback;
            completedSynchronously = true;
            XmlEncoding = xmlEncoding;
            UseEmptyParamsTag = useEmptyParamsTag;
            UseIndentation = useIndentation;
            Indentation = indentation;
            UseIntTag = useIntTag;
            UseStringTag = useStringTag;
        }

        internal void Complete(Exception ex)
        {
            Exception = ex;
            Complete();
        }

        internal void Complete()
        {
            try
            {
                ResponseStream?.Close();
                ResponseStream = null;

                if (ResponseBufferedStream != null)
                    ResponseBufferedStream.Position = 0;
            }
            catch (Exception ex)
            {
                if (Exception == null)
                    Exception = ex;
            }

            IsCompleted = true;

            try
            {
                manualResetEvent?.Set();
            }
            catch (Exception ex)
            {
                if (Exception == null)
                    Exception = ex;
            }

            userCallback?.Invoke(this);
        }

        internal WebResponse WaitForResponse()
        {
            if (!IsCompleted)
                AsyncWaitHandle.WaitOne();

            if (Exception != null)
                throw Exception;

            return Response;
        }

        internal bool EndSendCalled { get; set; } = false;

        internal byte[] Buffer { get; set; }

        internal WebRequest Request { get; }

        internal WebResponse Response { get; set; }

        internal Stream ResponseStream { get; set; }

        internal XmlRpcRequest XmlRpcRequest { get; set; }

        internal Stream ResponseBufferedStream { get; set; }

        internal Encoding XmlEncoding { get; }

        readonly AsyncCallback userCallback;
        bool completedSynchronously;
        ManualResetEvent manualResetEvent;
        internal CookieCollection _responseCookies;
        internal WebHeaderCollection _responseHeaders;
    }
}