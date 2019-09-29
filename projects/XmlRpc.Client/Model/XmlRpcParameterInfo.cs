using System;

namespace XmlRpc.Client.Model
{
    public class XmlRpcParameterInfo
    {
        public string Doc { get; set; }
        public bool IsParams { get; set; }
        public string XmlRpcName { get; set; }
        public Type Type { get; set; }
        public string XmlRpcType { get; set; }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                if (string.IsNullOrWhiteSpace(XmlRpcName))
                    XmlRpcName = _name;
            }
        }

        string _name;

        public XmlRpcParameterInfo() { }
    }
}