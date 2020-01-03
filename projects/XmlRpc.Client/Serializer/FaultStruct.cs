namespace XmlRpc.Client.Serializer
{
    class FaultStruct
    {
        public int faultCode;
        public string faultString;
    }

    class FaultStructStringCode
    {
        public string faultCode { get; set; }
        public string faultString { get; set; }
    }
}
