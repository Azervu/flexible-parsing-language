using FlexibleParsingLanguage.Parse;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Modules
{
    internal class FallbackModule : IReadingModule
    {
        public List<Type> HandledTypes => [typeof(IList), typeof(IDictionary)];

        public object ExtractValue(object? val)
        {
            return val;
        }

        public object Parse(object raw, string acc)
        {
            if (raw is IDictionary n)
                return n[acc];

#if DEBUG
            var t = raw?.GetType().Name ?? "null";
            throw new Exception($"Tried to string read {t}");
#endif

            return null;
        }

        public object Parse(object raw, int acc)
        {
            if (raw is IList a)
                return a[acc];

#if DEBUG
            throw new Exception($"Tried to Append to {raw?.GetType().FullName ?? "null"} ");
#endif

            return null;
        }

        IEnumerable<KeyValuePair<object, object>> IReadingModule.Foreach(object raw)
        {
            var result = new List<KeyValuePair<object, object>>();
            switch (raw)
            {
                case IList x:
                    for (var i = 0; i < x.Count; i++)
                        result.Add(new KeyValuePair<object, object>(i, x[i]));
                    return result;
                case IDictionary dict:
                    foreach (var key in dict.Keys)
                        result.Add(new KeyValuePair<object, object>(key, dict[key]));
                    return result;
                case IEnumerable it:
                    var n = 0;
                    foreach (var item in it)
                        result.Add(new KeyValuePair<object, object>(n++, item));
                    return result;
                default:
                    return result;
            }
        }
    }
}
