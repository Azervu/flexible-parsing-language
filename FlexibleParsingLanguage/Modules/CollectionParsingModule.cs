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
    internal class CollectionParsingModule : IReadingModule
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
            switch (raw)
            {
                case IList x:
                    for (var i = 0; i < x.Count; i++)
                        yield return new KeyValuePair<object, object>(i, x[i]);
                    break;
                case IDictionary x:
                    foreach (var k in x.Keys)
                        yield return new KeyValuePair<object, object>(k, x[k]);
                    break;
                default:
#if DEBUG
                    throw new Exception($"Tried to Foreach to {raw?.GetType().FullName ?? "null"} ");
#endif
                    break;
            }
        }
    }
}
