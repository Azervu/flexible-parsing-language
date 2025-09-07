using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Parse;

internal static class ParsingFocusDataExtension
{
    internal static List<SequenceIntersection<FocusEntry, FocusEntry>> GenerateSequencesIntersectionWriteRead(this ParsingFocusData data, int writeId, int readId)
    {
        var w = data.Writes[writeId];
        var r = data.Reads[readId];
        return data.GenerateSequencesIntersection(
            w, w.Select(x => x.SequenceId).ToList(),
            r, r.Select(x => x.SequenceId).ToList()
        );
    }

    internal static List<SequenceIntersection<ConfigEntry, FocusEntry>> GenerateSequencesIntersectionConfigRead(this ParsingFocusData data)
    {
        var w = data.Configs[data.Active.ConfigId];
        var r = data.Reads[data.Active.ReadId];

        return data.GenerateSequencesIntersection(
            w, w.Select(x => x.SequenceId).ToList(),
            r, r.Select(x => x.SequenceId).ToList()
        );
    }

    internal static List<SequenceIntersection<FocusEntry, ConfigEntry>> GenerateSequencesIntersectionReadConfig(this ParsingFocusData data)
    {
        var p = data.Reads[data.Active.ReadId];
        var a = data.Configs[data.Active.ConfigId];

        return data.GenerateSequencesIntersection(
            p, p.Select(x => x.SequenceId).ToList(),
            a, a.Select(x => x.SequenceId).ToList()
        );
    }
}
