using System;

namespace XmlRpc.Client.Attributes
{
    [AttributeUsage(AttributeTargets.ReturnValue)]
    public class XmlRpcReturnValueAttribute : Attribute
    {
        public string Description { get; set; }

        public XmlRpcReturnValueAttribute()
        {
            Description = string.Empty;
        }

        public override string ToString() => $"Description: {Description}";
    }
}