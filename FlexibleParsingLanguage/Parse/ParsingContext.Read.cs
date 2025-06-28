using FlexibleParsingLanguage.Compiler;

namespace FlexibleParsingLanguage.Parse;

internal partial class ParsingContext
{

    internal void ReadFunc(int opId, Func<IReadingModule, object, object> readTransform) => Focus.Read(opId, (r) => {
        UpdateReadModule(r);
#if DEBUG
        if (r.V == null)
            throw new Exception("Result is null");
#endif

        if (ReadingModule == null)
        {
            throw new Exception($"No reading module for type = '{r.V?.GetType().Name ?? "null"}'");
        }

        var result = readTransform(ReadingModule, r.V);
        return new KeyValuePair<ValueWrapper, ValueWrapper>(r, new ValueWrapper(result));
    });

    internal void ReadTransform(int opId, Func<FocusEntry, FocusEntry> readTransform) => Focus.ReadInner(opId, readTransform);

    internal void ReadTransformValue(int opId, Func<object, object> readTransform) => ReadTransform(opId, (focus) => new FocusEntry
    {
        Key = focus.Key, //TODO test focus.Value)
        Value = new ValueWrapper(readTransform(focus.Value.V)),
        SequenceId = focus.SequenceId
    });

    internal void ReadName(int opId) => ReadTransform(opId, (focus) => new FocusEntry
    {
        Key = focus.Key,
        Value = focus.Key,
        SequenceId = focus.SequenceId
    });
        
    internal void ReadFlatten(int opId) => Focus.ReadForeach(opId, (r) =>
    {
        UpdateReadModule(r.Value);
        return ReadingModule.Foreach(r.Value.V);
    });

    internal void UpdateReadModule(ValueWrapper obj)
    {
        var t = obj.V?.GetType() ?? typeof(void);
        if (t != _activeType)
        {
            _activeType = t;
            ReadingModule = _modules.LookupModule(t);
        }
    }


}