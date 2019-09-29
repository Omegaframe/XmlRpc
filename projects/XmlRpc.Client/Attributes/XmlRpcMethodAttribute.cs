using System;

namespace XmlRpc.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class XmlRpcMethodAttribute : Attribute
    {
        public string Method { get; } = string.Empty;
        public bool IntrospectionMethod { get; set; } = false;
        public bool StructParams { get; set; } = false;

        public string Description = string.Empty;
        public bool Hidden = false;

        public XmlRpcMethodAttribute() { }

        public XmlRpcMethodAttribute(string method)
        {
            Method = method;
        }

        public override string ToString()
        {
            return "Method : " + Method;
        }
    }
}