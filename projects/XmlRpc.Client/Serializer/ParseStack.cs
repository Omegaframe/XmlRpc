using System.Collections;

namespace XmlRpc.Client.Serializer
{
    public class ParseStack : Stack
    {
        public string ParseType { get; }

        public ParseStack(string parseType)
        {
            ParseType = parseType;
        }
    }
}
