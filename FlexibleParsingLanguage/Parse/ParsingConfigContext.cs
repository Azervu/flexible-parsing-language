using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Parse;

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
