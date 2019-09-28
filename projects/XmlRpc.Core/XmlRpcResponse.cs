using System;

namespace XmlRpc.Core
{
    public class XmlRpcResponse
    {
        public XmlRpcResponse()
        {
            retVal = null;
        }
        public XmlRpcResponse(object retValue)
        {
            retVal = retValue;
        }
        public Object retVal;
    }
}