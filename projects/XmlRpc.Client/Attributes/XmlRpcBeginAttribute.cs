using System;

namespace XmlRpc.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class XmlRpcBeginAttribute : Attribute
    {
        public string Method { get; } = string.Empty;
        public Type ReturnType { get; set; } = null;
        public bool IntrospectionMethod { get; set; } = false;

        public string Description = "";
        public bool Hidden = false;

        public XmlRpcBeginAttribute() { }

        public XmlRpcBeginAttribute(string method)
        {
            Method = method;
        }

        public override string ToString()
        {
            return "Method : " + Method;
        }
    }
}

