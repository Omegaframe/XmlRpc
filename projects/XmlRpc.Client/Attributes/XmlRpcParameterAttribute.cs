using System;

namespace XmlRpc.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class XmlRpcParameterAttribute : Attribute
    {
        public string Name { get; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public XmlRpcParameterAttribute() { }

        public XmlRpcParameterAttribute(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return "Description : " + Description;
        }
    }
}