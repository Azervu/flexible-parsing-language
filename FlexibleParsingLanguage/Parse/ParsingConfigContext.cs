using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Parse;



public class ParsingConfigContext
{
    public object Value { get; internal set; }
    public Dictionary<string, ParsingConfigContext> Entries { get; internal set; }



    public ParsingConfigContext(object value = null, Dictionary<string, ParsingConfigContext> entries = null)
    {
        Value = value;
        Entries = entries;
    }

    public ParsingConfigContext this[string index]
    {
        get => Entries[index];
    }




    internal void AddEntries(List<(string Key, string Value)> data)
    {
        if (Entries == null)
            Entries = new Dictionary<string, ParsingConfigContext>();

        foreach (var (k, v) in data)
        {
            if (!Entries.TryGetValue(k, out var category))
            {
                category = new ParsingConfigContext(null, new Dictionary<string, ParsingConfigContext>());
                Entries[k] = category;
            }
            category.Value = v;
        }
    }




    internal void AddContext(List<(string Key, string Value)> data)
    {
        var ctx = new ParsingConfigContext() { Entries = new Dictionary<string, ParsingConfigContext>() };
        foreach (var (k, v) in data)
        {
            ctx.Entries.Add(k, new ParsingConfigContext(v));

            if (!Entries.TryGetValue(k, out var category))
            {
                category = new ParsingConfigContext(null, new Dictionary<string, ParsingConfigContext>());
                Entries[k] = category;
            }
            category.Entries[v.ToString()] = ctx;
        }
    }
}



/*
public class ParsingConfigContext
{
    public ParsingConfigContext(Dictionary<string, string> config, List<Dictionary<string, string>> configEntries)
    {
        Data = config;
        var keys = new HashSet<string>();
        foreach (var entry in configEntries)
        {
            foreach (var (k, v) in entry)
            {
                if (!Entries.TryGetValue(k, out var entries))
                {
                    entries = new Dictionary<string, ParsingConfigContext>();
                    Entries.Add(k, entries);
                }

                entries.Add(v, new ParsingConfigContext(entry));
            }
        }
    }

    public ParsingConfigContext(Dictionary<string, string> config)
    {
        Data = config;
    }

    internal Dictionary<string, string> Data { get; set; }

    internal Dictionary<string, Dictionary<string, ParsingConfigContext>> Entries { get; set; }

}
*/