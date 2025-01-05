using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Parse;

internal static class ParsingFocusDataExtension
{
    internal static List<SequenceIntersection<FocusEntry, FocusEntry>> GenerateSequencesIntersectionWriteRead(this ParsingFocusData data)
    {
        var w = data.Writes[data.Active.WriteId];
        var r = data.Reads[data.Active.ReadId];

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




    internal static void WriteFromRead(this ParsingFocusData data, Func<FocusEntry, ValueWrapper> extractRead, Action<WriteParam> action)
    {
        var ws = data.GenerateSequencesIntersectionWriteRead();
        foreach (var rw in ws)
        {
            var write = rw.Primary;
            var read = rw.AVal;

            if (read.Foci.Count == 0)
                throw new Exception("no reads in sequence");
            var p = new WriteParam(read.Foci.Select(extractRead).ToList(), rw.Primary.Value, read.Multiread);
            action(p);
        }
    }


    internal static void WriteFlatten(this ParsingFocusData data, Func<ValueWrapper, ValueWrapper> writeTransform)
    {
        data.NextWrite(
            data.GenerateSequencesIntersectionWriteRead()
            .SelectMany(x => x.AVal.Foci.Select(r => new FocusEntry { SequenceId = r.SequenceId, Value = writeTransform(x.Primary.Value) }))
            .ToList()
        );
    }




}
