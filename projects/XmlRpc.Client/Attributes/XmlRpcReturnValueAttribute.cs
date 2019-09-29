using System;

namespace XmlRpc.Client.Attributes
{
    [AttributeUsage(AttributeTargets.ReturnValue)]
    public class XmlRpcReturnValueAttribute : Attribute
    {
        public string Description { get; set; } = string.Empty;

        public override string ToString()
        {
            return "Description : " + Description;
        }
    }
}