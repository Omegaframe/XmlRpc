using System;
using System.Reflection;

namespace XmlRpc.Client.Model
{
    public class XmlRpcMethodInfo : IComparable
    {
        public bool IsHidden { get; set; }

        public string Doc { get; set; } = string.Empty;

        public MethodInfo MethodInfo { get; set; }

        public string MiName { get; set; } = string.Empty;

        public XmlRpcParameterInfo[] Parameters { get; set; }

        public Type ReturnType { get; set; }

        public string ReturnXmlRpcType { get; set; }

        public string ReturnDoc { get; set; } = string.Empty;

        public string XmlRpcName { get; set; } = string.Empty;

        public XmlRpcMethodInfo() { }

        public int CompareTo(object obj)
        {
            var xmi = (XmlRpcMethodInfo)obj;
            return XmlRpcName.CompareTo(xmi.XmlRpcName);
        }
    }
}