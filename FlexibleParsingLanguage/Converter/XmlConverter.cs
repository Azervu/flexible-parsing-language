using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace FlexibleParsingLanguage.Converter;

internal class XmlConverter : IConverter
{
    public bool Convert(object input, out object result)
    {
        if (input is not string str)
            str = input.ToString();

        var xml = new XmlDocument();
        xml.LoadXml(str);
        result = xml;
        return true;
    }
}
