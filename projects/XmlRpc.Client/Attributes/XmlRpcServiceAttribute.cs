using System;

namespace XmlRpc.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class XmlRpcServiceAttribute : Attribute
    {
        public bool AutoDocumentation { get; set; } = true;
        public bool AutoDocVersion { get; set; } = true;
        public string Description { get; set; } = string.Empty;
        public int Indentation { get; set; } = 2;
        public bool Introspection { get; set; } = false;
        public string Name { get; set; } = string.Empty;
        public bool UseIndentation { get; set; } = true;
        public bool UseIntTag { get; set; } = false;
        public bool UseStringTag { get; set; } = true;
        public string XmlEncoding { get; set; } = null;

        public override string ToString()
        {
            return "Description : " + Description;
        }
    }
}