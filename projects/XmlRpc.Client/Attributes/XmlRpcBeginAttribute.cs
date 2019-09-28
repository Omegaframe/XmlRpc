using System;

namespace XmlRpc.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class XmlRpcBeginAttribute : Attribute
    {
        public XmlRpcBeginAttribute()
        {
        }

        public XmlRpcBeginAttribute(string method)
        {
            this.method = method;
        }

        public string Method
        {
            get
            { return method; }
        }

        public Type ReturnType
        {
            get { return returnType; }
            set { returnType = value; }
        }

        public bool IntrospectionMethod
        {
            get { return introspectionMethod; }
            set { introspectionMethod = value; }
        }

        public override string ToString()
        {
            string value = "Method : " + method;
            return value;
        }

        public string Description = "";
        public bool Hidden = false;
        string method = "";
        bool introspectionMethod = false;
        Type returnType = null;
    }
}

