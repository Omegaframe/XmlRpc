using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using XmlRpc.Client.Attributes;
using XmlRpc.Client.Exceptions;
using XmlRpc.Client.Model;
using XmlRpc.Client.Serializer.Request;
using XmlRpc.Client.Serializer.Response;

namespace XmlRpc.Client
{
    // todo: avoid own handling of streams etc.
    public class XmlRpcClientProtocol : IXmlRpcProxy
    {
        public XmlRpcClientProtocol()
        {
            Id = Guid.NewGuid();
        }

        public object Invoke(MethodBase methodBase, params object[] parameters)
        {
            return Invoke(this, methodBase as MethodInfo, parameters);
        }

        public object Invoke(MethodInfo methodInfo, params object[] parameters)
        {
            return Invoke(this, methodInfo, parameters);
        }

        public object Invoke(string methodName, params object[] parameters)
        {
            return Invoke(this, methodName, parameters);
        }

        public object Invoke(object clientObj, string methodName, params object[] parameters)
        {
            var methodInfo = GetMethodInfoFromName(clientObj, methodName, parameters);
            return Invoke(this, methodInfo, parameters);
        }

        public object Invoke(object clientObj, MethodInfo methodInfo, params object[] parameters)
        {
            ResponseHeaders = null;
            ResponseCookies = null;

            WebRequest webReq = null;
            object reto = null;
            try
            {
                var useUrl = GetEffectiveUrl(clientObj);
                webReq = GetWebRequest(new Uri(useUrl));
                var req = MakeXmlRpcRequest(webReq, methodInfo, parameters, XmlRpcMethod);

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

        public Guid Id { get; }

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

        XmlRpcRequest MakeXmlRpcRequest(WebRequest webRequest, MethodInfo methodInfo, object[] parameters, string xmlRpcMethod)
        {
            var rpcMethodName = GetRpcMethodName(methodInfo);

            webRequest.Method = HttpMethod.Post.Method;
            webRequest.ContentType = MediaTypeNames.Text.Xml;

            return new XmlRpcRequest(rpcMethodName, parameters, methodInfo, xmlRpcMethod, Id);
        }

        XmlRpcResponse ReadResponse(XmlRpcRequest request, WebResponse webResponse, Stream responseStream, Type returnType)
        {
            var httpResp = (HttpWebResponse)webResponse;
            if (httpResp.StatusCode != HttpStatusCode.OK)
            {
                if (httpResp.StatusCode == HttpStatusCode.BadRequest)
                    throw new XmlRpcException(httpResp.StatusDescription);
                else
                    throw new XmlRpcServerException(httpResp.StatusDescription);
            }

            var serializer = new XmlRpcResponseDeserializer();
            serializer.Configuration.NonStandard = NonStandard;

            var retType = returnType ?? request.mi.ReturnType;

            return serializer.DeserializeResponse(responseStream, retType);
        }

        public static MethodInfo GetMethodInfoFromName(object clientObj, string methodName, object[] parameters)
        {
            var paramTypes = new Type[0];
            if (parameters != null)
            {
                if (parameters.Any(p => p == null))
                    throw new XmlRpcNullParameterException("Null parameters are invalid");

                paramTypes = new Type[parameters.Length];
                for (int i = 0; i < paramTypes.Length; i++)
                    paramTypes[i] = parameters[i].GetType();
            }

            var type = clientObj.GetType();
            var methodInfo = type.GetMethod(methodName, paramTypes);
            if (methodInfo != null)
                return methodInfo;

            try
            {
                methodInfo = type.GetMethod(methodName);
            }
            catch (AmbiguousMatchException)
            {
                throw new XmlRpcInvalidParametersException("Method parameters match the signature of more than one method");
            }

            if (methodInfo == null)
                throw new Exception("Invoke on non-existent or non-public proxy method");

            throw new XmlRpcInvalidParametersException("Method parameters do not match signature of any method called " + methodName);
        }

        string GetRpcMethodName(MethodInfo methodInfo)
        {
            const string BeginPrefix = "Begin";

            string rpcMethod;
            var MethodName = methodInfo.Name;
            var attr = Attribute.GetCustomAttribute(methodInfo, typeof(XmlRpcBeginAttribute));
            if (attr != null)
            {
                rpcMethod = ((XmlRpcBeginAttribute)attr).Method;
                if (string.IsNullOrWhiteSpace(rpcMethod))
                {
                    if (!MethodName.StartsWith(BeginPrefix))
                        throw new Exception($"method {MethodName} has invalid signature for begin method");

                    rpcMethod = MethodName.Substring(MethodName.Length);
                }

                return rpcMethod;
            }

            // if no XmlRpcBegin attribute, must have XmlRpcMethod attribute   
            attr = Attribute.GetCustomAttribute(methodInfo, typeof(XmlRpcMethodAttribute));
            if (attr == null)
                throw new Exception("missing method attribute");

            var xrmAttr = attr as XmlRpcMethodAttribute;
            rpcMethod = xrmAttr.Method;
            if (string.IsNullOrWhiteSpace(rpcMethod))
                rpcMethod = methodInfo.Name;

            return rpcMethod;
        }

        public IAsyncResult BeginInvoke(MethodBase methodBase, object[] parameters, AsyncCallback callback, object outerAsyncState)
        {
            return BeginInvoke(methodBase as MethodInfo, parameters, this, callback, outerAsyncState);
        }

        public IAsyncResult BeginInvoke(MethodInfo methodInfo, object[] parameters, AsyncCallback callback, object outerAsyncState)
        {
            return BeginInvoke(methodInfo, parameters, this, callback, outerAsyncState);
        }

        public IAsyncResult BeginInvoke(string methodName, object[] parameters, object clientObj, AsyncCallback callback, object outerAsyncState)
        {
            var methodInfo = GetMethodInfoFromName(clientObj, methodName, parameters);
            return BeginInvoke(methodInfo, parameters, this, callback, outerAsyncState);
        }

        public IAsyncResult BeginInvoke(MethodInfo methodInfo, object[] parameters, object clientObj, AsyncCallback callback, object outerAsyncState)
        {
            var useUrl = GetEffectiveUrl(clientObj);
            var webReq = GetWebRequest(new Uri(useUrl));
            var xmlRpcReq = MakeXmlRpcRequest(webReq, methodInfo, parameters, XmlRpcMethod);
            SetProperties(webReq);
            SetRequestHeaders(Headers, webReq);

            SetClientCertificates(ClientCertificates, webReq);
            var asyncResult = new XmlRpcAsyncResult(this, xmlRpcReq, XmlEncoding, UseEmptyParamsTag, UseIndentation, Indentation, UseIntTag, UseStringTag, webReq, callback, outerAsyncState);
            webReq.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), asyncResult);

            if (!asyncResult.IsCompleted)
                asyncResult.CompletedSynchronously = false;

            return asyncResult;
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
            var type = clientObj.GetType();
            var useUrl = string.Empty;
            if (string.IsNullOrWhiteSpace(Url))
            {
                var urlAttr = Attribute.GetCustomAttribute(type, typeof(XmlRpcUrlAttribute));
                if (urlAttr is XmlRpcUrlAttribute xrsAttribute)
                    useUrl = xrsAttribute.Uri;
            }
            else
            {
                useUrl = Url;
            }
            if (string.IsNullOrWhiteSpace(useUrl))
                throw new XmlRpcMissingUrl("XmlRpcUrl attribute or Url property not set.");

            return useUrl;
        }

        [XmlRpcMethod("system.listMethods")]
        public string[] SystemListMethods()
        {
            return (string[])Invoke("SystemListMethods", new object[0]);
        }

        [XmlRpcMethod("system.listMethods")]
        public IAsyncResult BeginSystemListMethods(AsyncCallback callback, object state)
        {
            return BeginInvoke("SystemListMethods", new object[0], this, callback, state);
        }

        [XmlRpcMethod("system.multicall")]
        public string[] SystemMulticall()
        {
            return (string[])Invoke("SystemMulticall", new object[0]);
        }

        [XmlRpcMethod("system.multicall")]
        public IAsyncResult SystemMulticall(AsyncCallback callback, object state)
        {
            return BeginInvoke("SystemMulticall", new object[0], this, callback, state);
        }

        public string[] EndSystemListMethods(IAsyncResult asyncResult)
        {
            return (string[])EndInvoke(asyncResult);
        }

        [XmlRpcMethod("system.methodSignature")]
        public object[] SystemMethodSignature(string methodName)
        {
            return (object[])Invoke("SystemMethodSignature", new object[] { methodName });
        }

        [XmlRpcMethod("system.methodSignature")]
        public IAsyncResult BeginSystemMethodSignature(string methodName, AsyncCallback callback, object state)
        {
            return BeginInvoke("SystemMethodSignature", new object[] { methodName }, this, callback, state);
        }

        public Array EndSystemMethodSignature(IAsyncResult asyncResult)
        {
            return (Array)EndInvoke(asyncResult);
        }

        [XmlRpcMethod("system.methodHelp")]
        public string SystemMethodHelp(string methodName)
        {
            return (string)Invoke("SystemMethodHelp", new object[] { methodName });
        }

        [XmlRpcMethod("system.methodHelp")]
        public IAsyncResult BeginSystemMethodHelp(string methodName, AsyncCallback callback, object state)
        {
            return BeginInvoke("SystemMethodHelp", new object[] { methodName }, this, callback, state);
        }

        public string EndSystemMethodHelp(IAsyncResult asyncResult)
        {
            return (string)EndInvoke(asyncResult);
        }

        protected virtual WebRequest GetWebRequest(Uri uri)
        {
            return WebRequest.Create(uri);
        }

        protected virtual WebResponse GetWebResponse(WebRequest request)
        {
            try
            {
                return request.GetResponse();
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                    throw;

                return ex.Response;
            }
        }

        protected Stream MaybeDecompressStream(HttpWebResponse httpWebResp, Stream respStream)
        {
            var contentEncoding = httpWebResp.ContentEncoding?.ToLowerInvariant() ?? string.Empty;

            if (contentEncoding.Contains("gzip"))
                return new GZipStream(respStream, CompressionMode.Decompress);
            else if (contentEncoding.Contains("deflate"))
                return new DeflateStream(respStream, CompressionMode.Decompress);

            return respStream;
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


