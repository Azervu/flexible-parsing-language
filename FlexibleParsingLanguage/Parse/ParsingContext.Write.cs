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
                var value = focusEntry.Reads[i];
                var w = WritingModule.BlankMap();
                WritingModule.Append(focusEntry.Write, w);
                result.Add(new ParsingFocusEntry
                {
                    Reads = [value],
                    Write = w,
                    MultiRead = false,
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

                var t = TransformRead(read);
                result.Add(new ParsingFocusEntry
                {
                    Reads = [t],
                    Write = w,
                    MultiRead = false,
                });
            }
        }
        Focus = result;
    }

    internal void WriteStringFromRead(string acc)
    {
        WriteFromRead(x => TransformReadInner(x.Read), (m, f, r) => {
            m.Write(f.Write, acc, r);
        });
    }

    internal void WriteFromRead(Func<ParsingFocusRead, object> readFunc, Action<IWritingModule, ParsingFocusEntry, object> writeAction)
    {
        foreach (var focusEntry in Focus)
        {
            var r = focusEntry.MultiRead
                ? focusEntry.Reads.Select(readFunc).ToList()
                : readFunc(focusEntry.Reads[0]);

            writeAction(WritingModule, focusEntry, r);
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
                MultiRead = focusEntry.MultiRead,
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
                MultiRead = focusEntry.MultiRead,
            });
        }
        Focus = result;
    }
}