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
}