using System;
using XmlRpc.Client.Model;

namespace XmlRpc.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Class)]
    public class XmlRpcMissingMappingAttribute : Attribute
    {
        public MappingAction Action { get; } = MappingAction.Error;

        public XmlRpcMissingMappingAttribute() { }

        public XmlRpcMissingMappingAttribute(MappingAction action)
        {
            Action = action;
        }

        public override string ToString()
        {
            return Action.ToString();
        }
    }
}