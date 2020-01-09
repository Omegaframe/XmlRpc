using System.Text;
using System.Xml;
using XmlRpc.Client.Model;

namespace XmlRpc.Client.Serializer.Model
{
    public class SerializerConfig
    {
        public int Indentation { get; set; }
        public XmlRpcNonStandard NonStandard { get; set; }
        public bool UseEmptyParamsTag { get; set; }
        public bool UseIndentation { get; set; }
        public bool UseIntTag { get; set; }
        public bool UseStringTag { get; set; }
        public Encoding XmlEncoding { get; set; }
        public MappingAction MappingAction { get; set; }

        public SerializerConfig()
        {
            Indentation = 2;
            NonStandard = XmlRpcNonStandard.None;
            UseEmptyParamsTag = true;
            UseIndentation = true;
            UseIntTag = false;
            UseStringTag = true;
            XmlEncoding = null;
            MappingAction = MappingAction.Error;
        }

        public bool AllowInvalidHTTPContent()
            => (NonStandard & XmlRpcNonStandard.AllowInvalidHTTPContent) != 0;
        public bool AllowStringFaultCode()
            => (NonStandard & XmlRpcNonStandard.AllowStringFaultCode) != 0;
        public bool IgnoreDuplicateMembers()
            => (NonStandard & XmlRpcNonStandard.IgnoreDuplicateMembers) != 0;
        public bool MapEmptyDateTimeToMinValue()
            => (NonStandard & XmlRpcNonStandard.MapEmptyDateTimeToMinValue) != 0;
        public bool MapZerosDateTimeToMinValue()
            => (NonStandard & XmlRpcNonStandard.MapZerosDateTimeToMinValue) != 0;

        public void ConfigureXmlFormat(XmlTextWriter xtw)
        {
            if (UseIndentation)
            {
                xtw.Formatting = Formatting.Indented;
                xtw.Indentation = Indentation;
            }
            else
            {
                xtw.Formatting = Formatting.None;
            }
        }
    }
}
