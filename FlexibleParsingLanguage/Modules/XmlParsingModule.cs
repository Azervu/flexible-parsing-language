using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml;

namespace FlexibleParsingLanguage.Modules;

public class XmlParsingModule : IReadingModule
{
    public List<Type> HandledTypes { get; } = [typeof(XmlNode)];

    public object ExtractValue(object? val)
    {
        if (val is not XmlNode n)
            return val;

        return n.InnerText;
    }

    public object Parse(object raw, string acc)
    {
        if (raw is not XmlNode n)
            return null;

        return n[acc];
    }

    public object Parse(object raw, int acc)
    {
        if (raw is not XmlNode n)
            return null;

        return n.ChildNodes[acc];
    }

    IEnumerable<KeyValuePair<object, object>> IReadingModule.Foreach(object raw)
    {
        if (raw is XmlNode n)
        {
            foreach (XmlNode node in n.ChildNodes)
                yield return new KeyValuePair<object, object>(node.BaseURI, node);
        }
    }
}
