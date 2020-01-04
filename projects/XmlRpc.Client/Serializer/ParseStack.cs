using System.Collections;
using System.Text;

namespace XmlRpc.Client.Serializer
{
    public class ParseStack : Stack
    {
        public string ParseType { get; }

        public ParseStack(string parseType)
        {
            ParseType = parseType;
        }

        public string Dump()
        {
            var sb = new StringBuilder();

            foreach (var elem in this)
            {
                sb.Insert(0, elem);
                sb.Insert(0, " : ");
            }

            sb.Insert(0, ParseType);
            sb.Insert(0, "[");
            sb.Append("]");

            return sb.ToString();
        }
    }
}
