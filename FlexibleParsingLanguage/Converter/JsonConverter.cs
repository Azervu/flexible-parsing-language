using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Converter;

internal class JsonConverter : IConverter
{
    public object Convert(object input)
    {
        if (input is not string str)
            str = input.ToString();

        var r = JsonSerializer.Deserialize<JsonNode>(str);

        return r;
    }
}
