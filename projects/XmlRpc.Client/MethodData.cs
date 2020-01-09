using System;
using System.Reflection;

namespace XmlRpc.Client
{
    class MethodData
    {
        public MethodInfo Mi { get; }
        public string XmlRpcName { get; }
        public Type ReturnType { get; }
        public bool IsParamsMethod { get; }

        public MethodData(MethodInfo mi, string xmlRpcName, bool isParamsMethod)
        {
            Mi = mi;
            XmlRpcName = xmlRpcName;
            IsParamsMethod = isParamsMethod;
            ReturnType = null;
        }

        public MethodData(MethodInfo mi, string xmlRpcName, bool isParamsMethod, Type returnType) : this(mi, xmlRpcName, isParamsMethod)
        {
            ReturnType = returnType;
        }
    }
}
