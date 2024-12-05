using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Converter;

internal class JsonConverter : IConverter
{
    public object Convert(object input)
    {
        if (input is string str)
            return JsonSerializer.Serialize(str);
        return null;
    }
}
