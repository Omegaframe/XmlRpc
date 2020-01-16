using System;
using System.Reflection;

namespace XmlRpc.Client.Internals
{
    class MethodData
    {
        public MethodInfo MethodInfo { get; }
        public string XmlRpcName { get; }
        public Type ReturnType { get; }
        public bool IsParamsMethod { get; }

        public MethodData(MethodInfo mi, string xmlRpcName, bool isParamsMethod)
        {
            MethodInfo = mi;
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
