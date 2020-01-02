using System;

namespace XmlRpc.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class XmlRpcBeginAttribute : Attribute
    {
        public string Method { get; }
        public Type ReturnType { get; set; }
        public bool IntrospectionMethod { get; set; }
        public string Description { get; set; }
        public bool Hidden { get; set; }

        public XmlRpcBeginAttribute()
        {
            Method = string.Empty;
            ReturnType = null;
            IntrospectionMethod = false;
            Description = string.Empty;
            Hidden = false;
        }

        public XmlRpcBeginAttribute(string method) : base()
        {
            Method = method;
        }

        public override string ToString() => $"Method: {Method}";
    }
}

