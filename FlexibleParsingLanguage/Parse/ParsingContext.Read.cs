namespace FlexibleParsingLanguage.Parse;

internal partial class ParsingContext
{
    internal void ReadAction(Action<ParsingFocusRead> action)
    {
        foreach (var focusEntry in Focus)
        {
            foreach (var readEntry in focusEntry.Reads)
            {
                action(readEntry);
            }
        }
    }

    internal void ReadFunc(Func<IReadingModule, object, object> readTransform) => MapFocus((x) => ReadInner(x, readTransform));
    internal void ReadTransform(Func<ParsingFocusRead, ParsingFocusRead> readTransform) => MapFocus((focus) => new ParsingFocusEntry
    {
        Reads = focus.Reads.Select(readTransform).ToList(),
        Write = focus.Write,
        MultiRead = focus.MultiRead,
    });

    internal void ReadTransformValue(Func<object, object> readTransform) => ReadTransform((focus) => new ParsingFocusRead
    {
        Key = focus.Key,
        Config = focus.Config,
        Read = readTransform(focus.Read),

    });

    internal void ReadName() => MapFocus((focus) => new ParsingFocusEntry
    {
        Reads = focus.Reads.Select(x => new ParsingFocusRead
        {
            Key = x.Key,
            Read = x.Key,
            Config = x.Config,
        }).ToList(),
        Write = focus.Write,
        MultiRead = focus.MultiRead,
    });

    internal void ReadFlatten() => MapFocus(ReadFlattenInner);

    internal void ToRootRead()
    {
        var root = Store[1][0];
        MapFocus((f) => new ParsingFocusEntry
        {
            Reads = root.Reads,
            MultiRead = f.MultiRead,
            Write = f.Write,
        });
    }

    private ParsingFocusEntry ReadInner(ParsingFocusEntry focus, Func<IReadingModule, object, object> readTransform)
    {
        var newReads = new List<ParsingFocusRead>();
        foreach (var r in focus.Reads)
        {

            if (r.Read == null)
                throw new Exception("Trying to read null");


            UpdateReadModule(r.Read);

            var r2 = readTransform(ReadingModule, r.Read);

            newReads.Add(new ParsingFocusRead
            {
                Config = r.Config,
                Key = r.Read,
                Read = r2,
            });
        }
        return new ParsingFocusEntry
        {
            Reads = newReads,
            Write = focus.Write,
            MultiRead = focus.MultiRead,
        };
    }

    private ParsingFocusEntry ReadFlattenInner(ParsingFocusEntry focus)
    {
        var innerResult = new List<ParsingFocusRead>();
        foreach (var read in focus.Reads)
        {
            UpdateReadModule(read.Read);
            foreach (var (k, v) in ReadingModule.Foreach(read.Read))
                innerResult.Add(new ParsingFocusRead
                {
                    Config = read.Config,
                    Key = k,
                    Read = v,
                });

        }
        return new ParsingFocusEntry
        {
            Reads = innerResult,
            MultiRead = true,
            Write = focus.Write,
        };
    }

    private void UpdateReadModule(object obj)
    {
        var t = obj?.GetType() ?? typeof(void);
        if (t != _activeType)
        {
            _activeType = t;
            ReadingModule = _modules.LookupModule(t);
        }
    }

    private ParsingFocusRead TransformRead(ParsingFocusRead raw) => new ParsingFocusRead
    {
        Read = TransformReadInner(raw.Read),
        Key = raw.Key,
        Config = raw.Config,
    };

    internal object TransformReadInner(object raw)
    {
        UpdateReadModule(raw);
        if (ReadingModule == null)
            return raw;
        return ReadingModule.ExtractValue(raw);
    }
}