using System;

namespace XmlRpc.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class XmlRpcMemberAttribute : Attribute
    {
        public string Member { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public XmlRpcMemberAttribute() { }

        public XmlRpcMemberAttribute(string member)
        {
            Member = member;
        }

        public override string ToString()
        {
            return "Member : " + Member;
        }
    }
}