using System;

namespace XmlRpc.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class XmlRpcMemberAttribute : Attribute
    {
        public string Member { get; set; }
        public string Description { get; set; }

        public XmlRpcMemberAttribute()
        {
            Member = string.Empty;
            Description = string.Empty;
        }

        public XmlRpcMemberAttribute(string member) : base()
        {
            Member = member;
        }

        public override string ToString() => $"Member: {Member}";
    }
}