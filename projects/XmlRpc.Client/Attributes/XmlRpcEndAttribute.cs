using System;

namespace XmlRpc.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class XmlRpcEndAttribute : Attribute
    {
        public string Method { get; } = string.Empty;
        public bool IntrospectionMethod { get; set; } = false;

        public string Description = string.Empty;
        public bool Hidden = false;

        public XmlRpcEndAttribute() { }

        public XmlRpcEndAttribute(string method)
        {
            Method = method;
        }

        public override string ToString()
        {
            return "Method : " + Method;
        }
    }
}

