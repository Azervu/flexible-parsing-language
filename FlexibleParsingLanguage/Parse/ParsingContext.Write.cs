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
                result.Add(new ParsingFocusEntry
                {
                    Reads = [TransformRead(read)],
                    Write = w,
                    MultiRead = false,
                });
            }
        }
        Focus = result;
    }

    private object TransformReadInner(object raw)
    {
        UpdateReadModule(raw);
        if (ReadingModule == null)
            return raw;
        return ReadingModule.ExtractValue(raw);
    }

    internal void WriteFromRead(string acc)
    {
        foreach (var focusEntry in Focus)
        {
            var r = focusEntry.MultiRead
                ? focusEntry.Reads.Select(x => TransformReadInner(x.Read)).ToList()
                : TransformReadInner(focusEntry.Reads[0].Read);
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
                    WritingModule.Append(focusEntry.Write, TransformReadInner(r.Read));
                continue;
            }
            WritingModule.Append(focusEntry.Write, focusEntry.Reads[0].Read);
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