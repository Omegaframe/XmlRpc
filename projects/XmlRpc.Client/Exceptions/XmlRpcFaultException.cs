using System;
using System.Runtime.Serialization;

namespace XmlRpc.Client.Exceptions
{
    [Serializable]
    public class XmlRpcFaultException : ApplicationException
    {
        public int FaultCode { get; }
        public string FaultString { get; }

        public XmlRpcFaultException(int theCode, string theString) : base($"Server returned a fault exception: [{theCode}] {theString}")
        {
            FaultCode = theCode;
            FaultString = theString;
        }

        protected XmlRpcFaultException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            FaultCode = (int)info.GetValue("m_faultCode", typeof(int));
            FaultString = (string)info.GetValue("m_faultString", typeof(string));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("m_faultCode", FaultCode);
            info.AddValue("m_faultString", FaultString);
            base.GetObjectData(info, context);
        }
    }
}
