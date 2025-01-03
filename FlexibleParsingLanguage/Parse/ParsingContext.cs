using System.Collections;

namespace FlexibleParsingLanguage.Parse;

internal partial class ParsingContext
{
    internal IReadingModule ReadingModule;
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

    internal void WriteFlatten()
    {
        Focus.WriteFlatten((writeParent) =>
        {
            var w = WritingModule.BlankMap();
            WritingModule.Append(writeParent.V, w);
            return new ValueWrapper(w);
        });
    }

    internal void WriteFlattenArray()
    {
        Focus.WriteFlatten((writeParent) =>
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


    internal static void WriteAddRead(FplQuery parser, ParsingContext context, int intAcc, string acc)
    {



        /*
        
                    if (param.Write.V is IDictionary d)
            {
                var rng = new Random();
                d.Add(acc + "_" + rng.Next(99), "***");
            }
            else if (param.Write.V is IList l)
            {
                l.Add("acc");
            }



         */



        context.WriteFromRead((x) => context.TransformReadInner(x.Value), (w) =>
        {
            foreach (var r in w.Read)
                context.WritingModule.Append(w.Write.V, r.V);
        });
    }




    internal void WriteAction(Func<IWritingModule, ValueWrapper, ValueWrapper> writeFunc) => Focus.Write((data) => writeFunc(WritingModule, data));

    internal ValueWrapper TransformReadInner(ValueWrapper raw)
    {
        UpdateReadModule(raw);
        if (ReadingModule == null)
            return raw;
        var v = ReadingModule.ExtractValue(raw.V);

        return new ValueWrapper(v);
    }




}

