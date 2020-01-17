using System;
using System.Linq;
using XmlRpc.Client.Model;

namespace XmlRpc.Client.Attributes
{
    static class AttributeHelper
    {
        public static MappingAction MemberMappingAction(Type type, string memberName, MappingAction currentAction)
        {
            if (type == null)
                return currentAction;

            var fi = type.GetField(memberName);
            var pi = type.GetProperty(memberName);

            var attr = fi == null ?
                Attribute.GetCustomAttribute(pi, typeof(XmlRpcMissingMappingAttribute)) :
                Attribute.GetCustomAttribute(fi, typeof(XmlRpcMissingMappingAttribute));

            if (attr == null)
                return currentAction;

            return ((XmlRpcMissingMappingAttribute)attr).Action;
        }

        public static MappingAction StructMappingAction(Type type, MappingAction currentAction)
        {
            if (type == null)
                return currentAction;

            var attr = Attribute.GetCustomAttribute(type, typeof(XmlRpcMissingMappingAttribute));
            if (attr == null)
                return currentAction;

            return ((XmlRpcMissingMappingAttribute)attr).Action;
        }

        public static string GetStructName(Type valueType, string xmlRpcName)
        {
            if (valueType == null)
                return null;

            foreach (var field in valueType.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field, typeof(XmlRpcMemberAttribute)) as XmlRpcMemberAttribute;
                if (attribute.Member.Equals(xmlRpcName))
                    return field.Name;
            }

            foreach (var property in valueType.GetProperties())
            {
                var attribute = Attribute.GetCustomAttribute(property, typeof(XmlRpcMemberAttribute)) as XmlRpcMemberAttribute;
                if (attribute.Member.Equals(xmlRpcName))
                    return property.Name;
            }

            return null;
        }
    }
}
