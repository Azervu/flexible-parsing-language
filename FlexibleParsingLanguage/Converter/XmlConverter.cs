﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace FlexibleParsingLanguage.Converter;

internal class XmlConverter : IConverter
{
    public object Convert(object input)
    {
        if (input is not string str)
            return null;

        var xml = new XmlDocument();
        xml.LoadXml(str);
        return xml;
    }
}
