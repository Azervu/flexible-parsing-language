using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Parse;

internal partial class ParsingContext
{




    internal void WriteFlatten()
    {
        var result = new List<ParsingFocusEntry>();
        foreach (var focusEntry in Focus)
        {
            for (var i = 0; i < focusEntry.Reads.Count; i++)
            {
                var key = focusEntry.Keys?[i] ?? null;
                var value = focusEntry.Reads[i];
                var w = WritingModule.BlankMap();
                WritingModule.Append(focusEntry.Write, w);
                result.Add(new ParsingFocusEntry
                {
                    Keys = [key],
                    Reads = [value],
                    Write = w,
                    MultiRead = false,
                    Config = focusEntry.Config,
                });
            }
        }
        Focus = result;
    }

    internal void WriteFlattenArray()
    {
        var result = new List<ParsingFocusEntry>();
        foreach (var focusEntry in Focus)
        {
            foreach (var read in focusEntry.Reads)
            {
                var w = WritingModule.BlankArray();
                WritingModule.Append(focusEntry.Write, w);
                result.Add(new ParsingFocusEntry
                {
                    Keys = focusEntry.Keys,
                    Reads = [TransformRead(read)],
                    Write = w,
                    MultiRead = false,
                    Config = focusEntry.Config,
                });
            }
        }
        Focus = result;
    }

    internal void WriteFromRead(string acc)
    {
        foreach (var focusEntry in Focus)
        {
            //UpdateWriteModule(w);
            var r = focusEntry.MultiRead ? focusEntry.Reads.Select(TransformRead).ToList() : TransformRead(focusEntry.Reads[0]);
            WritingModule.Write(focusEntry.Write, acc, r);

        }
    }

    internal void WriteAddRead()
    {
        foreach (var focusEntry in Focus)
        {
            //UpdateWriteModule(w);
            if (focusEntry.MultiRead)
            {
                foreach (var r in focusEntry.Reads)
                    WritingModule.Append(focusEntry.Write, TransformRead(r));
                continue;
            }
            WritingModule.Append(focusEntry.Write, focusEntry.Reads[0]);
        }
    }

    internal void WriteTransform(Func<object, object> writeFunc)
    {
        var result = new List<ParsingFocusEntry>();
        foreach (var focusEntry in Focus)
        {
            result.Add(new ParsingFocusEntry
            {
                Reads = focusEntry.Reads,
                Write = writeFunc(focusEntry.Write),
                Keys = focusEntry.Keys,
                MultiRead = focusEntry.MultiRead,
                Config = focusEntry.Config,
            });
        }
        Focus = result;
    }

    internal void WriteAction(Func<IWritingModule, object, object> writeFunc)
    {
        var result = new List<ParsingFocusEntry>();
        foreach (var focusEntry in Focus)
        {
            result.Add(new ParsingFocusEntry
            {
                Reads = focusEntry.Reads,
                Write = writeFunc(WritingModule, focusEntry.Write),
                Keys = focusEntry.Keys,
                MultiRead = focusEntry.MultiRead,
                Config = focusEntry.Config,
            });
        }
        Focus = result;
    }
}
