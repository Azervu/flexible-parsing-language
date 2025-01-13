using FlexibleParsingLanguage.Converter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace FlexibleParsingLanguage.Converter;

internal class XmlConverter : IConverterFunction
{
    public string Name => "xml";

    public object Convert(object input)
    {
        if (input is not string str)
            str = input.ToString();

        var xml = new XmlDocument();
        xml.LoadXml(str);
        return xml;
    }
}