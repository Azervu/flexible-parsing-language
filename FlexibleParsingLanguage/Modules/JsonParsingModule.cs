using FlexibleParsingLanguage.Parse;
using System.Collections;
using System.Text.Json.Nodes;

namespace FlexibleParsingLanguage.Modules;

public class JsonParsingModule : IReadingModule
{
    public List<Type> HandledTypes { get; } = [typeof(JsonNode), typeof(JsonObject)];

    public object ExtractValue(object? val)
    {
        return val;
    }

    public object Parse(object raw, string acc)
    {
        if (raw is not JsonObject n)
        {
#if DEBUG
            throw new Exception($"tried to string access {raw?.GetType().FullName ?? "null"} ");
#endif
            return null;
        }

        var v = n[acc];
        return v;

    }

    public object Parse(object raw, int acc)
    {
        if (raw is not JsonArray a)
        {
#if DEBUG
            throw new Exception($"tried to int access {raw?.GetType().FullName ?? "null"} ");
#endif
            return null;
        }

        var v = a[acc];
        return v;
    }

    IEnumerable<KeyValuePair<object, object>> IReadingModule.Foreach(object raw)
    {
        switch (raw)
        {
            case JsonObject jsonNode:
                foreach (var x in jsonNode)
                    yield return new KeyValuePair<object, object>(x.Key, x.Value);
                break;
            case JsonArray jsonArray:
                for (int i = 0; i < jsonArray.Count; i++)
                    yield return new KeyValuePair<object, object>(i, jsonArray[i]);
                break;
            case ICollection<KeyValuePair<object, object>> dict:
                foreach (var kv in dict)
                    yield return kv;
                break;
            case IEnumerable enumerable:
                var ii = 0;
                foreach (var x in enumerable)
                {
                    yield return new KeyValuePair<object, object>(ii, x);
                    ii++;
                }
                break;
            default:
#if DEBUG
                throw new Exception($"tried to Json Foreach {raw?.GetType().FullName ?? "null"} ");
#endif
                break;
        }
    }
}
