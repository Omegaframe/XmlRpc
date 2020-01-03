using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using XmlRpc.Client.Attributes;
using XmlRpc.Client.Exceptions;
using XmlRpc.Client.Model;
using XmlRpc.Client.Serializer;

namespace XmlRpc.Client
{
    public class XmlRpcClientProtocol : Component, IXmlRpcProxy
    {
        Guid _id = Guid.NewGuid();

        public XmlRpcClientProtocol(IContainer container)
        {
            container.Add(this);
            InitializeComponent();
        }

        public XmlRpcClientProtocol()
        {
            InitializeComponent();
        }

        public object Invoke(MethodBase mb, params object[] Parameters)
        {
            return Invoke(this, mb as MethodInfo, Parameters);
        }

        public object Invoke(MethodInfo mi, params object[] Parameters)
        {
            return Invoke(this, mi, Parameters);
        }

        public object Invoke(string MethodName, params object[] Parameters)
        {
            return Invoke(this, MethodName, Parameters);
        }

        public object Invoke(object clientObj, string methodName, params object[] parameters)
        {
            var mi = GetMethodInfoFromName(clientObj, methodName, parameters);
            return Invoke(this, mi, parameters);
        }

        public object Invoke(object clientObj, MethodInfo mi, params object[] parameters)
        {
            ResponseHeaders = null;
            ResponseCookies = null;

            WebRequest webReq = null;
            object reto = null;
            try
            {
                var useUrl = GetEffectiveUrl(clientObj);
                webReq = GetWebRequest(new Uri(useUrl));
                var req = MakeXmlRpcRequest(webReq, mi, parameters, clientObj, XmlRpcMethod, _id);

                SetProperties(webReq);
                SetRequestHeaders(Headers, webReq);
                SetClientCertificates(ClientCertificates, webReq);

                Stream serStream = null;
                Stream reqStream = null;
                bool logging = (RequestEvent != null);
                if (!logging)
                    serStream = reqStream = webReq.GetRequestStream();
                else
                    serStream = new MemoryStream(2000);
                try
                {
                    var serializer = new XmlRpcRequestSerializer();
                    if (XmlEncoding != null)
                        serializer.Configuration.XmlEncoding = XmlEncoding;
                    serializer.Configuration.UseIndentation = UseIndentation;
                    serializer.Configuration.Indentation = Indentation;
                    serializer.Configuration.NonStandard = NonStandard;
                    serializer.Configuration.UseStringTag = UseStringTag;
                    serializer.Configuration.UseIntTag = UseIntTag;
                    serializer.Configuration.UseEmptyParamsTag = UseEmptyParamsTag;
                    serializer.SerializeRequest(serStream, req);
                    if (logging)
                    {
                        reqStream = webReq.GetRequestStream();
                        serStream.Position = 0;
                        serStream.CopyTo(reqStream);
                        reqStream.Flush();
                        serStream.Position = 0;
                        OnRequest(new XmlRpcRequestEventArgs(req.proxyId, req.number,
                          serStream));
                    }
                }
                finally
                {
                    if (reqStream != null)
                        reqStream.Close();
                }

                var webResp = GetWebResponse(webReq) as HttpWebResponse;

                ResponseCookies = webResp.Cookies;
                ResponseHeaders = webResp.Headers;

                Stream respStm = null;
                Stream deserStream;
                logging = (ResponseEvent != null);
                try
                {
                    respStm = webResp.GetResponseStream();
                    if (!logging)
                    {
                        deserStream = respStm;
                    }
                    else
                    {
                        deserStream = new MemoryStream(2000);
                        respStm.CopyTo(deserStream);
                        deserStream.Flush();
                        deserStream.Position = 0;
                    }

                    deserStream = MaybeDecompressStream(webResp, deserStream);

                    try
                    {
                        var resp = ReadResponse(req, webResp, deserStream, null);
                        reto = resp.retVal;
                    }
                    finally
                    {
                        if (logging)
                        {
                            deserStream.Position = 0;
                            OnResponse(new XmlRpcResponseEventArgs(req.proxyId, req.number, deserStream));
                        }
                    }
                }
                finally
                {
                    if (respStm != null)
                        respStm.Close();
                }
            }
            finally
            {
                if (webReq != null)
                    webReq = null;
            }
            return reto;
        }

        public bool AllowAutoRedirect { get; set; } = true;

        [Browsable(false)]
        public X509CertificateCollection ClientCertificates { get; } = new X509CertificateCollection();

        public string ConnectionGroupName { get; set; } = null;

        [Browsable(false)]
        public ICredentials Credentials { get; set; } = null;

        public bool EnableCompression { get; set; } = false;

        [Browsable(false)]
        public WebHeaderCollection Headers { get; } = new WebHeaderCollection();

        public bool Expect100Continue { get; set; } = false;

        public CookieContainer CookieContainer { get; } = new CookieContainer();

        public Guid Id => _id;

        public int Indentation { get; set; } = 2;

        public bool KeepAlive { get; set; } = true;

        public XmlRpcNonStandard NonStandard { get; set; } = XmlRpcNonStandard.None;

        public bool PreAuthenticate { get; set; } = false;

        [Browsable(false)]
        public System.Version ProtocolVersion { get; set; } = HttpVersion.Version11;

        [Browsable(false)]
        public IWebProxy Proxy { get; set; } = null;

        public CookieCollection ResponseCookies { get; private set; }

        public WebHeaderCollection ResponseHeaders { get; private set; }

        public int Timeout { get; set; } = 100000;

        public string Url { get; set; } = null;

        public bool UseEmptyParamsTag { get; set; } = true;

        public bool UseIndentation { get; set; } = true;

        public bool UseIntTag { get; set; } = false;

        public string UserAgent { get; set; } = "XML-RPC.NET";

        public bool UseStringTag { get; set; } = true;

        [Browsable(false)]
        public Encoding XmlEncoding { get; set; } = null;

        public string XmlRpcMethod { get; set; } = null;


        public void SetProperties(WebRequest webReq)
        {
            if (Proxy != null)
                webReq.Proxy = Proxy;

            var httpReq = (HttpWebRequest)webReq;
            httpReq.UserAgent = UserAgent;
            httpReq.ProtocolVersion = ProtocolVersion;
            httpReq.KeepAlive = KeepAlive;
            httpReq.CookieContainer = CookieContainer;
            httpReq.ServicePoint.Expect100Continue = Expect100Continue;
            httpReq.AllowAutoRedirect = AllowAutoRedirect;
            webReq.Timeout = Timeout;
            webReq.ConnectionGroupName = this.ConnectionGroupName;
            webReq.Credentials = Credentials;
            webReq.PreAuthenticate = PreAuthenticate;
            // Compact Framework sets this to false by default
            (webReq as HttpWebRequest).AllowWriteStreamBuffering = true;
            if (EnableCompression)
                webReq.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
        }

        void SetRequestHeaders(WebHeaderCollection headers, WebRequest webReq)
        {
            foreach (string key in headers)
                webReq.Headers.Add(key, headers[key]);
        }

        void SetClientCertificates(X509CertificateCollection certificates, WebRequest webReq)
        {
            foreach (X509Certificate certificate in certificates)
            {
                var httpReq = (HttpWebRequest)webReq;
                httpReq.ClientCertificates.Add(certificate);
            }
        }

        XmlRpcRequest MakeXmlRpcRequest(
            WebRequest webReq,
            MethodInfo mi,
            object[] parameters,
            object clientObj,
            string xmlRpcMethod,
            Guid proxyId)
        {
            webReq.Method = "POST";
            webReq.ContentType = "text/xml";
            string rpcMethodName = GetRpcMethodName(mi);
            var req = new XmlRpcRequest(rpcMethodName, parameters, mi, xmlRpcMethod, proxyId);
            return req;
        }

        XmlRpcResponse ReadResponse(
          XmlRpcRequest req,
          WebResponse webResp,
          Stream respStm,
          Type returnType)
        {
            var httpResp = (HttpWebResponse)webResp;
            if (httpResp.StatusCode != HttpStatusCode.OK)
            {
                if (httpResp.StatusCode == HttpStatusCode.BadRequest)
                    throw new XmlRpcException(httpResp.StatusDescription);
                else
                    throw new XmlRpcServerException(httpResp.StatusDescription);
            }
            var serializer = new XmlRpcResponseDeserializer();
            serializer.Configuration.NonStandard = NonStandard;
            var retType = returnType;
            if (retType == null)
                retType = req.mi.ReturnType;
            var xmlRpcResp = serializer.DeserializeResponse(respStm, retType);
            return xmlRpcResp;
        }

        public static MethodInfo GetMethodInfoFromName(object clientObj, string methodName, object[] parameters)
        {
            var paramTypes = new Type[0];
            if (parameters != null)
            {
                paramTypes = new Type[parameters.Length];
                for (int i = 0; i < paramTypes.Length; i++)
                {
                    if (parameters[i] == null)
                        throw new XmlRpcNullParameterException("Null parameters are invalid");
                    paramTypes[i] = parameters[i].GetType();
                }
            }
            var type = clientObj.GetType();
            var mi = type.GetMethod(methodName, paramTypes);
            if (mi == null)
            {
                try
                {
                    mi = type.GetMethod(methodName);
                }
                catch (AmbiguousMatchException)
                {
                    throw new XmlRpcInvalidParametersException("Method parameters match "
                      + "the signature of more than one method");
                }
                if (mi == null)
                    throw new Exception(
                      "Invoke on non-existent or non-public proxy method");
                else
                    throw new XmlRpcInvalidParametersException("Method parameters do "
                      + "not match signature of any method called " + methodName);
            }
            return mi;
        }

        string GetRpcMethodName(MethodInfo mi)
        {
            string rpcMethod;
            string MethodName = mi.Name;
            var attr = Attribute.GetCustomAttribute(mi, typeof(XmlRpcBeginAttribute));
            if (attr != null)
            {
                rpcMethod = ((XmlRpcBeginAttribute)attr).Method;
                if (rpcMethod == "")
                {
                    if (!MethodName.StartsWith("Begin") || MethodName.Length <= 5)
                        throw new Exception(String.Format(
                          "method {0} has invalid signature for begin method",
                          MethodName));
                    rpcMethod = MethodName.Substring(5);
                }
                return rpcMethod;
            }
            // if no XmlRpcBegin attribute, must have XmlRpcMethod attribute   
            attr = Attribute.GetCustomAttribute(mi, typeof(XmlRpcMethodAttribute));
            if (attr == null)
                throw new Exception("missing method attribute");

            var xrmAttr = attr as XmlRpcMethodAttribute;
            rpcMethod = xrmAttr.Method;
            if (rpcMethod == "")
                rpcMethod = mi.Name;

            return rpcMethod;
        }

        public IAsyncResult BeginInvoke(
          MethodBase mb,
          object[] parameters,
          AsyncCallback callback,
          object outerAsyncState)
        {
            return BeginInvoke(mb as MethodInfo, parameters, this, callback, outerAsyncState);
        }

        public IAsyncResult BeginInvoke(
          MethodInfo mi,
          object[] parameters,
          AsyncCallback callback,
          object outerAsyncState)
        {
            return BeginInvoke(mi, parameters, this, callback, outerAsyncState);
        }

        public IAsyncResult BeginInvoke(
          string methodName,
          object[] parameters,
          object clientObj,
          AsyncCallback callback,
          object outerAsyncState)
        {
            var mi = GetMethodInfoFromName(clientObj, methodName, parameters);
            return BeginInvoke(mi, parameters, this, callback, outerAsyncState);
        }

        public IAsyncResult BeginInvoke(
          MethodInfo mi,
          object[] parameters,
          object clientObj,
          AsyncCallback callback,
          object outerAsyncState)
        {
            var useUrl = GetEffectiveUrl(clientObj);
            var webReq = GetWebRequest(new Uri(useUrl));
            var xmlRpcReq = MakeXmlRpcRequest(webReq, mi, parameters, clientObj, XmlRpcMethod, _id);
            SetProperties(webReq);
            SetRequestHeaders(Headers, webReq);

            SetClientCertificates(ClientCertificates, webReq);
            Encoding useEncoding = null;
            if (XmlEncoding != null)
                useEncoding = XmlEncoding;
            var asr = new XmlRpcAsyncResult(
                this, xmlRpcReq, useEncoding, UseEmptyParamsTag, UseIndentation, Indentation,
                UseIntTag, UseStringTag, webReq, callback, outerAsyncState);
            webReq.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), asr);
            if (!asr.IsCompleted)
                asr.CompletedSynchronously = false;
            return asr;
        }

        static void GetRequestStreamCallback(IAsyncResult asyncResult)
        {
            XmlRpcAsyncResult clientResult = (XmlRpcAsyncResult)asyncResult.AsyncState;
            clientResult.CompletedSynchronously = asyncResult.CompletedSynchronously;
            try
            {
                Stream serStream = null;
                Stream reqStream = null;
                bool logging = (clientResult.ClientProtocol.RequestEvent != null);
                if (!logging)
                {
                    serStream = reqStream = clientResult.Request.EndGetRequestStream(asyncResult);
                }
                else
                    serStream = new MemoryStream(2000);
                try
                {
                    var req = clientResult.XmlRpcRequest;
                    var serializer = new XmlRpcRequestSerializer();
                    if (clientResult.XmlEncoding != null)
                        serializer.Configuration.XmlEncoding = clientResult.XmlEncoding;
                    serializer.Configuration.UseEmptyParamsTag = clientResult.UseEmptyParamsTag;
                    serializer.Configuration.UseIndentation = clientResult.UseIndentation;
                    serializer.Configuration.Indentation = clientResult.Indentation;
                    serializer.Configuration.UseIntTag = clientResult.UseIntTag;
                    serializer.Configuration.UseStringTag = clientResult.UseStringTag;
                    serializer.SerializeRequest(serStream, req);
                    if (logging)
                    {
                        reqStream = clientResult.Request.EndGetRequestStream(asyncResult);
                        serStream.Position = 0;
                        serStream.CopyTo(reqStream);
                        reqStream.Flush();
                        serStream.Position = 0;
                        clientResult.ClientProtocol.OnRequest(
                          new XmlRpcRequestEventArgs(req.proxyId, req.number, serStream));
                    }
                }
                finally
                {
                    if (reqStream != null)
                        reqStream.Close();
                }
                clientResult.Request.BeginGetResponse(new AsyncCallback(GetResponseCallback), clientResult);
            }
            catch (Exception ex)
            {
                ProcessAsyncException(clientResult, ex);
            }
        }

        static void GetResponseCallback(IAsyncResult asyncResult)
        {
            var result = (XmlRpcAsyncResult)asyncResult.AsyncState;
            result.CompletedSynchronously = asyncResult.CompletedSynchronously;
            try
            {
                result.Response = result.ClientProtocol.GetWebResponse(result.Request, asyncResult);
            }
            catch (Exception ex)
            {
                ProcessAsyncException(result, ex);
                if (result.Response == null)
                    return;
            }
            ReadAsyncResponse(result);
        }

        static void ReadAsyncResponse(XmlRpcAsyncResult result)
        {
            if (result.Response.ContentLength == 0)
            {
                result.Complete();
                return;
            }
            try
            {
                result.ResponseStream = result.Response.GetResponseStream();
                ReadAsyncResponseStream(result);
            }
            catch (Exception ex)
            {
                ProcessAsyncException(result, ex);
            }
        }

        static void ReadAsyncResponseStream(XmlRpcAsyncResult result)
        {
            IAsyncResult asyncResult;
            do
            {
                byte[] buff = result.Buffer;
                long contLen = result.Response.ContentLength;
                if (buff == null)
                {
                    if (contLen == -1)
                        result.Buffer = new byte[1024];
                    else
                        result.Buffer = new byte[contLen];
                }
                else
                {
                    if (contLen != -1 && contLen > result.Buffer.Length)
                        result.Buffer = new byte[contLen];
                }
                buff = result.Buffer;
                asyncResult = result.ResponseStream.BeginRead(buff, 0, buff.Length,
                  new AsyncCallback(ReadResponseCallback), result);
                if (!asyncResult.CompletedSynchronously)
                    return;
            }
            while (!(ProcessAsyncResponseStreamResult(result, asyncResult)));
        }

        static bool ProcessAsyncResponseStreamResult(XmlRpcAsyncResult result, IAsyncResult asyncResult)
        {
            var endReadLen = result.ResponseStream.EndRead(asyncResult);
            var contLen = result.Response.ContentLength;
            bool completed;
            if (endReadLen == 0)
                completed = true;
            else if (contLen > 0 && endReadLen == contLen)
            {
                result.ResponseBufferedStream = new MemoryStream(result.Buffer);
                completed = true;
            }
            else
            {
                if (result.ResponseBufferedStream == null)
                {
                    result.ResponseBufferedStream = new MemoryStream(result.Buffer.Length);
                }
                result.ResponseBufferedStream.Write(result.Buffer, 0, endReadLen);
                completed = false;
            }
            if (completed)
                result.Complete();
            return completed;
        }


        static void ReadResponseCallback(IAsyncResult asyncResult)
        {
            var result = (XmlRpcAsyncResult)asyncResult.AsyncState;
            result.CompletedSynchronously = asyncResult.CompletedSynchronously;
            if (asyncResult.CompletedSynchronously)
                return;
            try
            {
                bool completed = ProcessAsyncResponseStreamResult(result, asyncResult);
                if (!completed)
                    ReadAsyncResponseStream(result);
            }
            catch (Exception ex)
            {
                ProcessAsyncException(result, ex);
            }
        }

        static void ProcessAsyncException(XmlRpcAsyncResult clientResult, Exception ex)
        {
            var webex = ex as WebException;
            if (webex != null && webex.Response != null)
            {
                clientResult.Response = webex.Response;
                return;
            }
            if (clientResult.IsCompleted)
                throw new Exception("error during async processing");
            clientResult.Complete(ex);
        }

        public object EndInvoke(IAsyncResult asr)
        {
            return EndInvoke(asr, null);
        }

        public object EndInvoke(IAsyncResult asr, Type returnType)
        {
            object reto = null;
            Stream responseStream = null;
            try
            {
                var clientResult = (XmlRpcAsyncResult)asr;
                if (clientResult.Exception != null)
                    throw clientResult.Exception;
                if (clientResult.EndSendCalled)
                    throw new Exception("dup call to EndSend");

                clientResult.EndSendCalled = true;
                var webResp = (HttpWebResponse)clientResult.WaitForResponse();

                clientResult._responseCookies = webResp.Cookies;
                clientResult._responseHeaders = webResp.Headers;

                responseStream = clientResult.ResponseBufferedStream;
                if (ResponseEvent != null)
                {
                    OnResponse(new XmlRpcResponseEventArgs(
                      clientResult.XmlRpcRequest.proxyId,
                      clientResult.XmlRpcRequest.number,
                      responseStream));
                    responseStream.Position = 0;
                }

                responseStream = MaybeDecompressStream(webResp, responseStream);

                var resp = ReadResponse(clientResult.XmlRpcRequest, webResp, responseStream, returnType);
                reto = resp.retVal;
            }
            finally
            {
                if (responseStream != null)
                    responseStream.Close();
            }
            return reto;
        }

        string GetEffectiveUrl(object clientObj)
        {
            Type type = clientObj.GetType();
            // client can either have define URI in attribute or have set it
            // via proxy's ServiceURI property - but must exist by now
            string useUrl = "";
            if (string.IsNullOrWhiteSpace(Url))
            {
                var urlAttr = Attribute.GetCustomAttribute(type, typeof(XmlRpcUrlAttribute));
                if (urlAttr != null)
                {
                    var xrsAttr = urlAttr as XmlRpcUrlAttribute;
                    useUrl = xrsAttr.Uri;
                }
            }
            else
            {
                useUrl = Url;
            }
            if (string.IsNullOrWhiteSpace(useUrl))
                throw new XmlRpcMissingUrl("Proxy XmlRpcUrl attribute or Url property not set.");

            return useUrl;
        }

        [XmlRpcMethod("system.listMethods")]
        public string[] SystemListMethods()
        {
            return (string[])Invoke("SystemListMethods", new object[0]);
        }

        [XmlRpcMethod("system.listMethods")]
        public IAsyncResult BeginSystemListMethods(AsyncCallback Callback, object State)
        {
            return BeginInvoke("SystemListMethods", new object[0], this, Callback, State);
        }

        public string[] EndSystemListMethods(IAsyncResult AsyncResult)
        {
            return (string[])EndInvoke(AsyncResult);
        }

        [XmlRpcMethod("system.methodSignature")]
        public object[] SystemMethodSignature(string MethodName)
        {
            return (object[])Invoke("SystemMethodSignature", new object[] { MethodName });
        }

        [XmlRpcMethod("system.methodSignature")]
        public IAsyncResult BeginSystemMethodSignature(string MethodName, AsyncCallback Callback, object State)
        {
            return BeginInvoke("SystemMethodSignature", new object[] { MethodName }, this, Callback, State);
        }

        public Array EndSystemMethodSignature(IAsyncResult AsyncResult)
        {
            return (Array)EndInvoke(AsyncResult);
        }

        [XmlRpcMethod("system.methodHelp")]
        public string SystemMethodHelp(string MethodName)
        {
            return (string)Invoke("SystemMethodHelp", new object[] { MethodName });
        }

        [XmlRpcMethod("system.methodHelp")]
        public IAsyncResult BeginSystemMethodHelp(string MethodName, AsyncCallback Callback, object State)
        {
            return BeginInvoke("SystemMethodHelp", new object[] { MethodName }, this, Callback, State);
        }

        public string EndSystemMethodHelp(IAsyncResult AsyncResult)
        {
            return (string)EndInvoke(AsyncResult);
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        void InitializeComponent()
        {
        }

        protected virtual WebRequest GetWebRequest(Uri uri)
        {
            WebRequest req = WebRequest.Create(uri);
            return req;
        }

        protected virtual WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse ret;
            try
            {
                ret = request.GetResponse();
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                    throw;
                ret = ex.Response;
            }
            return ret;
        }

        // support for gzip and deflate
        protected Stream MaybeDecompressStream(HttpWebResponse httpWebResp, Stream respStream)
        {
            Stream decodedStream;
            string contentEncoding = httpWebResp.ContentEncoding?.ToLower() ?? string.Empty;
            if (contentEncoding.Contains("gzip"))
                decodedStream = new GZipStream(respStream, CompressionMode.Decompress);
            else if (contentEncoding.Contains("deflate"))
                decodedStream = new DeflateStream(respStream, CompressionMode.Decompress);
            else
                decodedStream = respStream;
            return decodedStream;
        }

        protected virtual WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            return request.EndGetResponse(result);
        }

        public event XmlRpcRequestEventHandler RequestEvent;
        public event XmlRpcResponseEventHandler ResponseEvent;


        protected virtual void OnRequest(XmlRpcRequestEventArgs e)
        {
            RequestEvent?.Invoke(this, e);
        }

        internal bool LogResponse
        {
            get { return ResponseEvent != null; }
        }

        protected virtual void OnResponse(XmlRpcResponseEventArgs e)
        {
            ResponseEvent?.Invoke(this, e);
        }

        internal void InternalOnResponse(XmlRpcResponseEventArgs e)
        {
            OnResponse(e);
        }
    }


    public delegate void XmlRpcRequestEventHandler(object sender, XmlRpcRequestEventArgs args);

    public delegate void XmlRpcResponseEventHandler(object sender, XmlRpcResponseEventArgs args);
}


