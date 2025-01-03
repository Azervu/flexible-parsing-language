namespace FlexibleParsingLanguage.Parse;

internal partial class ParsingContext
{

    internal void ReadFunc(Func<IReadingModule, object, object> readTransform) => Focus.Read((r) => {
        UpdateReadModule(r);
#if DEBUG
        if (r.V == null)
            throw new Exception("Result is null");
#endif
        var result = readTransform(ReadingModule, r.V);
        return new KeyValuePair<ValueWrapper, ValueWrapper>(r, new ValueWrapper(result));
    });


    internal void ReadTransform(Func<FocusEntry, FocusEntry> readTransform) => Focus.ReadInner(readTransform);

    internal void ReadTransformValue(Func<object, object> readTransform) => ReadTransform((focus) => new FocusEntry
    {
        Key = focus.Key, //TODO test focus.Value)
        Value = new ValueWrapper(readTransform(focus.Value.V)),
        SequenceId = focus.SequenceId
    });

    internal void ReadName() => ReadTransform((focus) => new FocusEntry
    {
        Key = focus.Key,
        Value = focus.Key,
        SequenceId = focus.SequenceId
    });
        
    internal void ReadFlatten() => Focus.ReadForeach((r) =>
    {
        UpdateReadModule(r);
        return ReadingModule.Foreach(r.V);
    });

    private ParsingFocusEntry ReadFlattenInner(ParsingFocusEntry focus)
    {
        var innerResult = new List<ParsingFocusRead>();
        foreach (var read in focus.Reads)
        {
            UpdateReadModule(new ValueWrapper(read.Read));
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










    internal void ToRootRead()
    {
        Focus.LoadRead(Compiler.Compiler.RootId);
    }



    private void UpdateReadModule(ValueWrapper obj)
    {
        var t = obj.V?.GetType() ?? typeof(void);
        if (t != _activeType)
        {
            _activeType = t;
            ReadingModule = _modules.LookupModule(t);
        }
    }

    private ParsingFocusRead TransformRead(ParsingFocusRead raw) => new ParsingFocusRead
    {
        Read = TransformReadInner(new ValueWrapper(raw.Read)),
        Key = raw.Key,
        Config = raw.Config,
    };

}