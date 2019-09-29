﻿using System;

namespace XmlRpc.Client.Attributes
{
    public enum MappingAction
    {
        Ignore,
        Error
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Struct
       | AttributeTargets.Property | AttributeTargets.Class)]
    public class XmlRpcMissingMappingAttribute : Attribute
    {
        public XmlRpcMissingMappingAttribute()
        {
        }

        public XmlRpcMissingMappingAttribute(MappingAction action)
        {
            _action = action;
        }

        public MappingAction Action
        {
            get
            { return _action; }
        }

        public override string ToString()
        {
            string value = _action.ToString();
            return value;
        }

        MappingAction _action = MappingAction.Error;
    }
}