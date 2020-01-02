using System;

namespace XmlRpc.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class XmlRpcEndAttribute : Attribute
    {
        public string Method { get; }
        public bool IntrospectionMethod { get; set; }
        public string Description { get; set; }
        public bool Hidden { get; set; }

        public XmlRpcEndAttribute()
        {
            Method = string.Empty;
            IntrospectionMethod = false;
            Description = string.Empty;
            Hidden = false;
        }

        public XmlRpcEndAttribute(string method) : base()
        {
            Method = method;
        }

        public override string ToString() => $"Method: {Method}";
    }
}

