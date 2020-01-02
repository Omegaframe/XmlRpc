using System;

namespace XmlRpc.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class XmlRpcMethodAttribute : Attribute
    {
        public string Method { get; }
        public bool IntrospectionMethod { get; set; }
        public bool StructParams { get; set; }
        public string Description { get; set; }
        public bool Hidden { get; set; }

        public XmlRpcMethodAttribute()
        {
            Method = string.Empty;
            IntrospectionMethod = false;
            StructParams = false;
            Description = string.Empty;
            Hidden = false;
        }

        public XmlRpcMethodAttribute(string method) : base()
        {
            Method = method;
        }

        public override string ToString() => $"Method: {Method}";
    }
}