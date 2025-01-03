using FlexibleParsingLanguage.Compiler;

namespace FlexibleParsingLanguage.Parse;

internal partial struct ParsesOperationType
{
    internal Action<FplQuery, ParsingContext, int, string> Op { get; private set; }

    internal ParsesOperationType(Action<FplQuery, ParsingContext, int, string> op)
    {
        if (op == null)
            throw new ArgumentNullException(nameof(op));
        Op = op;
    }

    internal OpCompileType GetOpCompileType() => GetMetaData().OpCompileType;

    internal OperationMetaData GetMetaData() => Ops[Op];

    internal static Dictionary<Action<FplQuery, ParsingContext, int, string>, OperationMetaData> Ops = new Dictionary<Action<FplQuery, ParsingContext, int, string>, OperationMetaData>
    {

        { Read, new OperationMetaData(OpCompileType.WriteObject, nameof(Read)) },
        { Write, new OperationMetaData(OpCompileType.WriteObject, nameof(Write)) },
        { WriteFromRead, new OperationMetaData(OpCompileType.WriteObject, nameof(WriteFromRead)) },

        { ReadInt, new OperationMetaData(OpCompileType.WriteArray, nameof(ReadInt)) },

        { WriteArray, new OperationMetaData(OpCompileType.WriteArray, nameof(WriteArray)) },
        { WriteArrayInt, new OperationMetaData(OpCompileType.WriteArray, nameof(WriteArrayInt)) },


        { WriteRoot, new OperationMetaData(OpCompileType.None, nameof(WriteRoot)) },
        { Save, new OperationMetaData(OpCompileType.None, nameof(Save)) },
        { Load, new OperationMetaData(OpCompileType.None, nameof(Load)) },
        { ReadName, new OperationMetaData(OpCompileType.None, nameof(ReadName)) },
        { WriteInt, new OperationMetaData(OpCompileType.None, nameof(WriteInt)) },

        { WriteFlatten, new OperationMetaData(OpCompileType.None, nameof(WriteFlatten)) },
        { Function, new OperationMetaData(OpCompileType.None, nameof(Function)) },
        { ParamLiteral, new OperationMetaData(OpCompileType.None, nameof(ParamLiteral)) },
        { ParamToRead, new OperationMetaData(OpCompileType.None, nameof(ParamToRead)) },
        { ParamFromRead, new OperationMetaData(OpCompileType.None, nameof(ParamFromRead)) },
        { Lookup, new OperationMetaData(OpCompileType.None, nameof(Lookup)) },
        { ChangeLookup, new OperationMetaData(OpCompileType.None, nameof(ChangeLookup)) },
        { LookupRead, new OperationMetaData(OpCompileType.None, nameof(LookupRead)) },
        { LookupReadValue, new OperationMetaData(OpCompileType.None, nameof(LookupReadValue)) },
    };

    internal static void Read(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.ReadFunc((m, readSrc) => m.Parse(readSrc, acc));

    internal static void Write(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.WriteAction((m, writeHeader) =>
    {
        var w = m.BlankMap();
        m.Write(writeHeader.V, acc, w);
        return new ValueWrapper(w);
    });

    internal static void ReadInt(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.ReadFunc((m, readSrc) => m.Parse(readSrc, intAcc));

    internal static void WriteArray(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.WriteAction((m, writeHeader) =>
    {
        var w2 = m.BlankArray();
        m.Write(writeHeader.V, acc, w2);
        return new ValueWrapper(w2);
    });

    internal static void WriteArrayInt(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.WriteAction((m, writeHeader) =>
    {
        var w1 = m.BlankArray();
        m.Write(writeHeader.V, intAcc, w1);
        return new ValueWrapper(w1);
    });

    internal static void WriteRoot(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.Focus.LoadWrite(Compiler.Compiler.RootId);
    internal static void Save(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.Focus.Save(intAcc);
    internal static void Load(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.Focus.Load(intAcc);
    internal static void ReadName(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.ReadName();
    internal static void WriteInt(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.WriteAction((m, writeHeader) =>
    {
        var w = context.WritingModule.BlankMap();
        m.Write(writeHeader.V, intAcc, w);
        return new ValueWrapper(w);
    });

    internal static void WriteFromRead(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.WriteStringFromRead(acc);

    internal static void WriteFlatten(FplQuery parser, ParsingContext context, int intAcc, string acc)
    {
        switch (intAcc)
        {
            case 1:
                context.WriteFlatten();
                break;
            case 2:
                context.WriteFlattenArray();
                break;
        }
    }

    internal static void Function(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.ReadTransformValue(parser._converter[acc].Convert);

    internal static void ParamLiteral(FplQuery parser, ParsingContext context, int intAcc, string acc) => throw new NotImplementedException("Lookup");

    /*
    context.ReadAction((r) =>
    {

        r.Param = acc;
    });
*/

    internal static void ParamToRead(FplQuery parser, ParsingContext context, int intAcc, string acc) => throw new NotImplementedException("Lookup");
    /*
context.ReadTransform((r) =>
new ParsingFocusRead
{
    Config = r.Config,
    Key = r.Key,
    Read = r.Param,
});

*/
    internal static void ParamFromRead(FplQuery parser, ParsingContext context, int intAcc, string acc) => throw new NotImplementedException("Lookup");

    //context.ReadAction((r) => { r.Param = r.Read; });
    internal static void Lookup(FplQuery parser, ParsingContext context, int intAcc, string acc) => throw new NotImplementedException("Lookup");
    //context.ReadAction((r) => { r.Param = r.Config[r.Param?.ToString()].Value; });


    internal static void ChangeLookup(FplQuery parser, ParsingContext context, int intAcc, string acc) => throw new NotImplementedException("Lookup");
    //context.ReadAction((r) => { r.Config = r.Config[r.Param?.ToString()]; });

    internal static void LookupRead(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.ReadTransform((f) => {
        throw new NotImplementedException("Lookup");
        
        /*
        var c = f.Config[f.Read.ToString()];
        return new ParsingFocusRead
        {
            Config = c,
            Key = f.Key,
            Read = f.Read,
        };
        */
    });

    internal static void LookupReadValue(FplQuery parser, ParsingContext context, int intAcc, string acc) => throw new NotImplementedException("Lookup");
    //context.WriteFromRead(x => x.Config.Value, (m, f, r) => { m.Append(f.Write, r); });
}

internal struct OperationMetaData
{
    internal OpCompileType OpCompileType;
    internal string Name { get; private set; }
    internal OperationMetaData(OpCompileType wt, string name)
    {
        OpCompileType = wt;
        Name = name;
    }
}