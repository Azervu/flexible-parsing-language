using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Modules;

public class JsonParsingModule : IReadingModule
{
    public List<Type> HandledTypes { get; } = [typeof(JsonNode), typeof(JsonObject)];

    public object Foreach(object raw, int acc)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<(object key, object value)> Foreach(object raw)
    {
        var d = new Dictionary<object, object>();
        switch (raw)
        {
            case JsonObject jsonNode:
                foreach (var x in jsonNode)
                    yield return (x.Key, x.Value);
                break;
            case JsonArray jsonArray:
                for (int i = 0; i < jsonArray.Count; i++)
                    yield return (i, jsonArray[i]);
                break;
            case ICollection<KeyValuePair<object, object>> dict:
                foreach (var kv in dict)
                    yield return (kv.Key, kv.Value);
                break;
            case IEnumerable enumerable:
                var ii = 0;
                foreach (var x in enumerable)
                {
                    yield return (ii, x);
                    ii++;
                }
                break;
        }
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
