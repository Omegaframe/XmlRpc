using System;
using System.Xml;
using XmlRpc.Client.DataTypes;
using XmlRpc.Client.Model;

namespace XmlRpc.Server.Protocol
{
    static class XmlRpcDocWriter
    {
        public static void WriteDoc(XmlWriter writer, Type type, bool autoDocVersion)
        {
            var serviceInfo = XmlRpcServiceInfo.CreateServiceInfo(type);

            writer.WriteStartElement("html");

            WriteHead(writer, serviceInfo.Name);
            WriteBody(writer, type, autoDocVersion);

            writer.WriteEndElement();
        }

        static void WriteHead(XmlWriter writer, string title)
        {
            writer.WriteStartElement("head");

            WriteStyle(writer);
            WriteTitle(writer, title);

            writer.WriteEndElement();
        }

        static void WriteBody(XmlWriter writer, Type type, bool autoDocVersion)
        {
            writer.WriteStartElement("body");

            WriteType(writer, type);

            WriteFooter(writer, autoDocVersion);

            writer.WriteEndElement();
        }

        static void WriteFooter(XmlWriter writer, bool autoDocVersion)
        {
            if (!autoDocVersion)
                return;

            writer.WriteStartElement("div");
            writer.WriteAttributeString("id", "content");
            writer.WriteStartElement("hr");
            writer.WriteEndElement();

            var assemblyName = typeof(XmlRpcServerProtocol).Assembly.GetName();
            writer.WriteString($"{assemblyName.Name} - Version {assemblyName.Version.Major}.{assemblyName.Version.Minor}.{assemblyName.Version.Build}");

            writer.WriteEndElement();
        }

        static void WriteType(XmlWriter writer, Type type)
        {
            writer.WriteStartElement("div");
            writer.WriteAttributeString("id", "content");

            var serviceInfo = XmlRpcServiceInfo.CreateServiceInfo(type);

            writer.WriteStartElement("p");
            writer.WriteAttributeString("class", "heading1");
            writer.WriteString(serviceInfo.Name);
            writer.WriteEndElement();
            writer.WriteStartElement("br");
            writer.WriteEndElement();

            if (!string.IsNullOrWhiteSpace(serviceInfo.Doc))
            {
                writer.WriteStartElement("p");
                writer.WriteAttributeString("class", "intro");
                writer.WriteString(serviceInfo.Doc);
                writer.WriteEndElement();
            }

            writer.WriteStartElement("p");
            writer.WriteAttributeString("class", "intro");
            writer.WriteString("The following methods are supported:");
            writer.WriteEndElement();

            writer.WriteStartElement("ul");

            foreach (var methodInfo in serviceInfo.Methods)
            {
                if (methodInfo.IsHidden)
                    continue;

                writer.WriteStartElement("li");
                writer.WriteStartElement("a");
                writer.WriteAttributeString("href", "#" + methodInfo.XmlRpcName);
                writer.WriteString(methodInfo.XmlRpcName);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            writer.WriteEndElement();

            foreach (var methodInfo in serviceInfo.Methods)
            {
                if (!methodInfo.IsHidden)
                    WriteMethod(writer, methodInfo);
            }

            writer.WriteEndElement();
        }

        static void WriteMethod(XmlWriter writer, XmlRpcMethodInfo methodInfos)
        {
            writer.WriteStartElement("span");

            writer.WriteStartElement("h2");
            writer.WriteStartElement("a");
            writer.WriteAttributeString("name", $"#{methodInfos.XmlRpcName}");
            writer.WriteString($"method {methodInfos.XmlRpcName}");
            writer.WriteEndElement();
            writer.WriteEndElement();

            if (!string.IsNullOrWhiteSpace(methodInfos.Doc))
            {
                writer.WriteStartElement("p");
                writer.WriteAttributeString("class", "intro");
                writer.WriteString(methodInfos.Doc);
                writer.WriteEndElement();
            }

            writer.WriteStartElement("h3");
            writer.WriteString("Parameters");
            writer.WriteEndElement();

            writer.WriteStartElement("table");
            writer.WriteAttributeString("cellspacing", "0");
            writer.WriteAttributeString("cellpadding", "5");
            writer.WriteAttributeString("width", "90%");

            if (methodInfos.Parameters.Length > 0)
            {
                foreach (var parameterInfo in methodInfos.Parameters)
                {
                    writer.WriteStartElement("tr");
                    writer.WriteStartElement("td");
                    writer.WriteAttributeString("width", "33%");
                    WriteType(writer, parameterInfo.Type, parameterInfo.IsParams);
                    writer.WriteEndElement();

                    writer.WriteStartElement("td");
                    if (string.IsNullOrWhiteSpace(parameterInfo.Doc))
                    {
                        writer.WriteString(parameterInfo.Name);
                    }
                    else
                    {
                        writer.WriteString(parameterInfo.Name);
                        writer.WriteString(" - ");
                        writer.WriteString(parameterInfo.Doc);
                    }
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
            }
            else
            {
                writer.WriteStartElement("tr");
                writer.WriteStartElement("td");
                writer.WriteAttributeString("width", "33%");
                writer.WriteString("none");
                writer.WriteEndElement();
                writer.WriteStartElement("td");
                writer.WriteString("&nbsp;");
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("h3");
            writer.WriteString("Return Value");
            writer.WriteEndElement();

            writer.WriteStartElement("table");
            writer.WriteAttributeString("cellspacing", "0");
            writer.WriteAttributeString("cellpadding", "5");
            writer.WriteAttributeString("width", "90%");

            writer.WriteStartElement("tr");

            writer.WriteStartElement("td");
            writer.WriteAttributeString("width", "33%");
            WriteType(writer, methodInfos.ReturnType, false);
            writer.WriteEndElement();

            writer.WriteStartElement("td");
            if (!string.IsNullOrWhiteSpace(methodInfos.ReturnDoc))
                writer.WriteString(methodInfos.ReturnDoc);
            else
                writer.WriteString(string.Empty);
            writer.WriteEndElement();

            writer.WriteEndElement();

            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        static void WriteStyle(XmlWriter writer)
        {
            writer.WriteStartElement("style");
            writer.WriteAttributeString("type", "text/css");

            writer.WriteString("BODY { color: #000000; background-color: white; font-family: Verdana; margin-left: 0px; margin-top: 0px; }");
            writer.WriteString("#content { margin-left: 30px; font-size: .70em; padding-bottom: 2em; }");
            writer.WriteString("A:link { color: #336699; font-weight: bold; text-decoration: underline; }");
            writer.WriteString("A:visited { color: #6699cc; font-weight: bold; text-decoration: underline; }");
            writer.WriteString("A:active { color: #336699; font-weight: bold; text-decoration: underline; }");
            writer.WriteString("A:hover { color: cc3300; font-weight: bold; text-decoration: underline; }");
            writer.WriteString("P { color: #000000; margin-top: 0px; margin-bottom: 12px; font-family: Verdana; }");
            writer.WriteString("pre { background-color: #e5e5cc; padding: 5px; font-family: Courier New; font-size: x-small; margin-top: -5px; border: 1px #f0f0e0 solid; }");
            writer.WriteString("td { color: #000000; font-family: Verdana; font-size: .7em; border: solid 1px;  }");
            writer.WriteString("h2 { font-size: 1.5em; font-weight: bold; margin-top: 25px; margin-bottom: 10px; border-top: 1px solid #003366; margin-left: -15px; color: #003366; }");
            writer.WriteString("h3 { font-size: 1.1em; color: #000000; margin-left: -15px; margin-top: 10px; margin-bottom: 10px; }");
            writer.WriteString("ul, ol { margin-top: 10px; margin-left: 20px; }");
            writer.WriteString("li { margin-top: 10px; color: #000000; }");
            writer.WriteString("font.value { color: darkblue; font: bold; }");
            writer.WriteString("font.key { color: darkgreen; font: bold; }");
            writer.WriteString(".heading1 { color: #ffffff; font-family: Tahoma; font-size: 26px; font-weight: normal; background-color: #003366; margin-top: 0px; margin-bottom: 0px; margin-left: -30px; padding-top: 10px; padding-bottom: 3px; padding-left: 15px; width: 105%; }");
            writer.WriteString(".intro { margin-left: -15px; }");
            writer.WriteString("table { border: solid 1px; }");

            writer.WriteEndElement();
        }

        static void WriteTitle(XmlWriter writer, string title)
        {
            writer.WriteStartElement("title");
            writer.WriteString(title);
            writer.WriteEndElement();
        }

        static void WriteType(XmlWriter writer, Type type, bool isParams)
        {
            // TODO: following is hack for case when type is object
            string xmlRpcType;
            if (!isParams)
            {
                if (type != typeof(object))
                    xmlRpcType = XmlRpcServiceInfo.GetXmlRpcTypeString(type);
                else
                    xmlRpcType = "any";
            }
            else
            {
                xmlRpcType = "varargs";
            }

            writer.WriteString(xmlRpcType);
            if (xmlRpcType == "struct" && type != typeof(XmlRpcStruct))
            {
                writer.WriteString(" ");
                writer.WriteStartElement("a");
                writer.WriteAttributeString("href", "#" + type.Name);
                writer.WriteString(type.Name);
                writer.WriteEndElement();
            }
            else if (xmlRpcType == "array" || xmlRpcType == "varargs")
            {
                if (type.GetArrayRank() == 1)  // single dim array
                {
                    writer.WriteString(" of ");
                    var elemType = type.GetElementType();

                    string elemXmlRpcType;
                    if (elemType != typeof(object))
                        elemXmlRpcType = XmlRpcServiceInfo.GetXmlRpcTypeString(elemType);
                    else
                        elemXmlRpcType = "any";

                    writer.WriteString(elemXmlRpcType);
                    if (elemXmlRpcType == "struct" && elemType != typeof(XmlRpcStruct))
                    {
                        writer.WriteString(" ");
                        writer.WriteStartElement("a");
                        writer.WriteAttributeString("href", "#" + elemType.Name);
                        writer.WriteString(elemType.Name);
                        writer.WriteEndElement();
                    }
                }
            }
        }
    }
}