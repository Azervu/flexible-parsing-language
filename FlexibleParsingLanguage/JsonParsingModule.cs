using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage;

public class JsonParsingModule : IReadingModule
{
    public List<Type> HandledTypes { get; } = [typeof(JsonNode), typeof(JsonObject[])];

    public object Foreach(object raw, int acc)
    {
        throw new NotImplementedException();
    }

    public IEnumerable Foreach(object raw)
    {
        if (raw is IEnumerable it)
            return it;

        return null;
    }

    public object Parse(object raw, string acc)
    {
        if (raw is JsonObject n)
            return n[acc];

        return null;
    }

    public object Parse(object raw, int acc)
    {
        if (raw is JsonArray a)
            return a[acc];

        return null;
    }
}
