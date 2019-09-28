using System;
using System.Runtime.Serialization;

namespace XmlRpc.Client.Exceptions
{
    [Serializable]
    public class XmlRpcFaultException : ApplicationException
    {

        public XmlRpcFaultException(int TheCode, string TheString)
          : base("Server returned a fault exception: [" + TheCode.ToString() +
                  "] " + TheString)
        {
            FaultCode = TheCode;
            FaultString = TheString;
        }

        protected XmlRpcFaultException(
          SerializationInfo info,
          StreamingContext context)
          : base(info, context)
        {
            FaultCode = (int)info.GetValue("m_faultCode", typeof(int));
            FaultString = (String)info.GetValue("m_faultString", typeof(string));
        }

        public int FaultCode { get; }

        public string FaultString { get; }

        // public methods
        //
        public override void GetObjectData(
          SerializationInfo info,
          StreamingContext context)
        {
            info.AddValue("m_faultCode", FaultCode);
            info.AddValue("m_faultString", FaultString);
            base.GetObjectData(info, context);
        }
    }
}
