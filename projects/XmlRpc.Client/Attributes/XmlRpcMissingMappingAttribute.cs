using System;
using XmlRpc.Client.Model;

namespace XmlRpc.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Class)]
    public class XmlRpcMissingMappingAttribute : Attribute
    {
        public MappingAction Action { get; }

        public XmlRpcMissingMappingAttribute()
        {
            Action = MappingAction.Error;
        }

        public XmlRpcMissingMappingAttribute(MappingAction action) : base()
        {
            Action = action;
        }

        public override string ToString() => Action.ToString();
    }
}