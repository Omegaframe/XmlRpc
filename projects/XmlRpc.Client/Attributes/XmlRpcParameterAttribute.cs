using System;

namespace XmlRpc.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class XmlRpcParameterAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; set; }

        public XmlRpcParameterAttribute()
        {
            Name = string.Empty;
            Description = string.Empty;
        }

        public XmlRpcParameterAttribute(string name) : base()
        {
            Name = name;
        }

        public override string ToString() => $"Description: {Description}";
    }
}