using System;

namespace XmlRpc.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class XmlRpcUrlAttribute : Attribute
    {
        public string Uri { get; }

        public XmlRpcUrlAttribute(string UriString)
        {
            Uri = UriString;
        }

        public override string ToString()
        {
            return "Uri : " + Uri;
        }
    }
}
