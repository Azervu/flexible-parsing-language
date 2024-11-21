using System;
using System.Collections;
using System.Collections.Generic;
using System.Formats.Tar;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage;

internal class CollectionWritingModule : IWritingModule
{
    public List<Type> HandledTypes { get; } = [typeof(IDictionary)];


    public object Root() => new Dictionary<string, object>();

    public void Write(object raw, string acc, object val)
    {
        if (raw is not IDictionary dict)
            return;

        dict.Add(acc, val);
    }

    public void Write(object raw, int acc, object val)
    {
        if (raw is not IDictionary dict)
            return;

        dict.Add(acc.ToString(), val);
    }
}
