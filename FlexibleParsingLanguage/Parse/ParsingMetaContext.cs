namespace FlexibleParsingLanguage.Parse;

public class ParsingMetaContext
{
    public object Value { get; internal set; }
    public Dictionary<string, ParsingMetaContext> Entries { get; internal set; }

    public ParsingMetaContext(object value = null, Dictionary<string, ParsingMetaContext> entries = null)
    {
        Value = value;
        Entries = entries;
    }

    internal void AddEntries(List<(string Key, string Value)> data)
    {
        if (Entries == null)
            Entries = new Dictionary<string, ParsingMetaContext>();

        foreach (var (k, v) in data)
        {
            if (!Entries.TryGetValue(k, out var category))
            {
                category = new ParsingMetaContext(null, new Dictionary<string, ParsingMetaContext>());
                Entries[k] = category;
            }
            category.Value = v;
        }
    }

    internal void AddContext(List<(string Key, string Value)> data)
    {
        var ctx = new ParsingMetaContext() { Entries = new Dictionary<string, ParsingMetaContext>() };
        foreach (var (k, v) in data)
        {
            ctx.Entries.Add(k, new ParsingMetaContext(v));

            if (!Entries.TryGetValue(k, out var category))
            {
                category = new ParsingMetaContext(null, new Dictionary<string, ParsingMetaContext>());
                Entries[k] = category;
            }
            category.Entries[v.ToString()] = ctx;
        }
    }
}