using System;

namespace XmlRpc.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class XmlRpcServiceAttribute : Attribute
    {
        public bool AutoDocumentation { get; set; }
        public bool AutoDocVersion { get; set; }
        public string Description { get; set; }
        public int Indentation { get; set; }
        public bool Introspection { get; set; }
        public string Name { get; set; }
        public bool UseIndentation { get; set; }
        public bool UseIntTag { get; set; }
        public bool UseStringTag { get; set; }
        public string XmlEncoding { get; set; }

        public XmlRpcServiceAttribute()
        {
            AutoDocumentation = true;
            AutoDocVersion = true;
            Description = string.Empty;
            Indentation = 2;
            Introspection = false;
            Name = string.Empty;
            UseIndentation = true;
            UseIntTag = false;
            UseStringTag = true;
            XmlEncoding = null;
        }

        public override string ToString() => $"Description: {Description}";
    }
}