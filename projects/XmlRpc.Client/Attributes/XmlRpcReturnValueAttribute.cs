using System;

namespace XmlRpc.Client.Attributes
{
    [AttributeUsage(AttributeTargets.ReturnValue)]
    public class XmlRpcReturnValueAttribute : Attribute
    {
        public string Description
        {
            get { return description; }
            set { description = value; }
        }
        public override string ToString()
        {
            string value = "Description : " + description;
            return value;
        }
        private string description = "";
    }
}