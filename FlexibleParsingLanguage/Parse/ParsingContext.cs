using System.Text;

namespace FlexibleParsingLanguage.Parse;

internal partial class ParsingContext
{
    internal IWritingModule WritingModule;
    internal ParsingFocusData Focus;
    private Type _activeType = null;
    private ModuleHandler _modules;

    public ParsingContext(
        IWritingModule writingModule,
        ModuleHandler modules,
        object readRoot,
        object writeRoot,
        ParsingMetaContext parsingConfig
    )
    {
        _modules = modules;
        Focus = new ParsingFocusData(parsingConfig, readRoot, writeRoot);
        WritingModule = writingModule;
    }

    internal void WriteFlatten(int opId)
    {
        Focus.WriteFlatten(opId, (writeParent) =>
        {
            var w = WritingModule.BlankMap();
            WritingModule.Append(writeParent.V, w);
            return new ValueWrapper(w);
        });
    }

    internal void WriteFlattenArray(int opId)
    {
        Focus.WriteFlatten(opId, (writeParent) =>
        {
            var w = WritingModule.BlankArray();
            WritingModule.Append(writeParent.V, w);
            return new ValueWrapper(w);
        });
    }

    internal void WriteStringFromRead(string acc)
    {
        WriteFromRead(x => TransformReadInner(x.Value), (param) => {
            if (param.MultiRead)
                WritingModule.Write(param.Write.V, acc, param.Read.Select(x => x.V).ToList());
            else
                WritingModule.Write(param.Write.V, acc, param.Read[0].V);
        });
    }

    internal void WriteFromRead(Func<FocusEntry, ValueWrapper> readFunc, Action<WriteParam> writeAction) => Focus.WriteFromRead(readFunc, writeAction);

    internal static void WriteAddRead(FplQuery parser, ParsingContext context, ParseOperationData d)
    {
        context.WriteFromRead((x) => context.TransformReadInner(x.Value), (w) =>
        {
            foreach (var r in w.Read)
                context.WritingModule.Append(w.Write.V, r.V);
        });
    }

    internal void WriteAction(int opId, Func<IWritingModule, ValueWrapper, ValueWrapper> writeFunc) => Focus.Write(opId, (data) => writeFunc(WritingModule, data));

    internal IReadingModule GetReadingModule(ValueWrapper obj)
    {
        var t = obj.V?.GetType() ?? typeof(void);
        return _modules.LookupModule(t);
    }

    internal IReadingModule GetReadingModuleFromValue(object v)
    {
        return _modules.LookupModule(v?.GetType() ?? typeof(void));
    }


    internal ValueWrapper TransformReadInner(ValueWrapper raw)
    {
        var v = GetReadingModule(raw).ExtractValue(raw.V);
        return new ValueWrapper(v);
    }


}