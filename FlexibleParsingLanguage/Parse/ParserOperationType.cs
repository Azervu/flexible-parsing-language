namespace FlexibleParsingLanguage.Parse;

internal partial struct ParsesOperationType
{
    internal Action<Parser, ParsingContext, int, string> Op { get; private set; }

    internal ParsesOperationType(Action<Parser, ParsingContext, int, string> op)
    {
        if (op == null)
            throw new ArgumentNullException(nameof(op));
        Op = op;
    }

    internal WriteType GetWriteType() => GetMetaData().WriteType;

    internal OperationMetaData GetMetaData() => Ops[Op];

    internal static Dictionary<Action<Parser, ParsingContext, int, string>, OperationMetaData> Ops = new Dictionary<Action<Parser, ParsingContext, int, string>, OperationMetaData>
    {

        { Read, new OperationMetaData(WriteType.Object, nameof(Read)) },
        { Write, new OperationMetaData(WriteType.Object, nameof(Write)) },
        { WriteFromRead, new OperationMetaData(WriteType.Object, nameof(WriteFromRead)) },

        { ReadInt, new OperationMetaData(WriteType.Array, nameof(ReadInt)) },
        { ReadFlatten, new OperationMetaData(WriteType.Array, nameof(ReadFlatten)) },
        { WriteArray, new OperationMetaData(WriteType.Array, nameof(WriteArray)) },
        { WriteArrayInt, new OperationMetaData(WriteType.Array, nameof(WriteArrayInt)) },

        { WriteAddRead, new OperationMetaData(WriteType.None, nameof(WriteAddRead)) },
        { ReadRoot, new OperationMetaData(WriteType.None, nameof(ReadRoot)) },
        { WriteRoot, new OperationMetaData(WriteType.None, nameof(WriteRoot)) },
        { Save, new OperationMetaData(WriteType.None, nameof(Save)) },
        { Load, new OperationMetaData(WriteType.None, nameof(Load)) },
        { ReadName, new OperationMetaData(WriteType.None, nameof(ReadName)) },
        { WriteInt, new OperationMetaData(WriteType.None, nameof(WriteInt)) },

        { WriteFlatten, new OperationMetaData(WriteType.None, nameof(WriteFlatten)) },
        { Function, new OperationMetaData(WriteType.None, nameof(Function)) },
        { ParamLiteral, new OperationMetaData(WriteType.None, nameof(ParamLiteral)) },
        { ParamToRead, new OperationMetaData(WriteType.None, nameof(ParamToRead)) },
        { ParamFromRead, new OperationMetaData(WriteType.None, nameof(ParamFromRead)) },
        { Lookup, new OperationMetaData(WriteType.None, nameof(Lookup)) },
        { ChangeLookup, new OperationMetaData(WriteType.None, nameof(ChangeLookup)) },
        { LookupRead, new OperationMetaData(WriteType.None, nameof(LookupRead)) },
        { LookupReadValue, new OperationMetaData(WriteType.None, nameof(LookupReadValue)) },
    };

    internal static void WriteAddRead(Parser parser, ParsingContext context, int intAcc, string acc)
    {
        foreach (var focusEntry in context.Focus)
        {
            //UpdateWriteModule(w);
            if (focusEntry.MultiRead)
            {
                foreach (var r in focusEntry.Reads)
                    context.WritingModule.Append(focusEntry.Write, context.TransformReadInner(r.Read));
                continue;
            }
            context.WritingModule.Append(focusEntry.Write, focusEntry.Reads[0].Read);
        }
    }

    internal static void Read(Parser parser, ParsingContext context, int intAcc, string acc) => context.ReadFunc((m, readSrc) => m.Parse(readSrc, acc));

    internal static void Write(Parser parser, ParsingContext context, int intAcc, string acc) => context.WriteAction((m, writeHeader) =>
    {
        var w = m.BlankMap();
        m.Write(writeHeader, acc, w);
        return w;
    });

    internal static void ReadInt(Parser parser, ParsingContext context, int intAcc, string acc) => context.ReadFunc((m, readSrc) => m.Parse(readSrc, intAcc));

    internal static void ReadFlatten(Parser parser, ParsingContext context, int intAcc, string acc) => context.ReadFlatten();

    internal static void WriteArray(Parser parser, ParsingContext context, int intAcc, string acc) => context.WriteAction((m, writeHeader) =>
    {
        var w2 = m.BlankArray();
        m.Write(writeHeader, acc, w2);
        return w2;
    });

    internal static void WriteArrayInt(Parser parser, ParsingContext context, int intAcc, string acc) => context.WriteAction((m, writeHeader) =>
    {
        var w1 = m.BlankArray();
        m.Write(writeHeader, intAcc, w1);
        return w1;
    });

    internal static void ReadRoot(Parser parser, ParsingContext context, int intAcc, string acc) => context.ToRootRead();

    internal static void WriteRoot(Parser parser, ParsingContext context, int intAcc, string acc) => context.ToRootWrite();

    internal static void Save(Parser parser, ParsingContext context, int intAcc, string acc) => context.Store[intAcc] = context.Focus;

    internal static void Load(Parser parser, ParsingContext context, int intAcc, string acc) => context.Focus = context.Store[intAcc];

    internal static void ReadName(Parser parser, ParsingContext context, int intAcc, string acc) => context.ReadName();
    internal static void WriteInt(Parser parser, ParsingContext context, int intAcc, string acc) => context.WriteAction((m, writeHeader) =>
    {
        var w = context.WritingModule.BlankMap();
        m.Write(writeHeader, intAcc, w);
        return w;
    });

    internal static void WriteFromRead(Parser parser, ParsingContext context, int intAcc, string acc) => context.WriteStringFromRead(acc);

    internal static void WriteFlatten(Parser parser, ParsingContext context, int intAcc, string acc)
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

    internal static void Function(Parser parser, ParsingContext context, int intAcc, string acc) => context.ReadTransformValue(parser._converter[acc].Convert);

    internal static void ParamLiteral(Parser parser, ParsingContext context, int intAcc, string acc) => context.ReadAction((r) =>
    {
        r.Param = acc;
    });

    internal static void ParamToRead(Parser parser, ParsingContext context, int intAcc, string acc) => context.ReadTransform((r) => new ParsingFocusRead
    {
        Config = r.Config,
        Key = r.Key,
        Read = r.Param,
    });

    internal static void ParamFromRead(Parser parser, ParsingContext context, int intAcc, string acc) => context.ReadAction((r) => { r.Param = r.Read; });
    internal static void Lookup(Parser parser, ParsingContext context, int intAcc, string acc) => context.ReadAction((r) => { r.Param = r.Config[r.Param?.ToString()].Value; });
    internal static void ChangeLookup(Parser parser, ParsingContext context, int intAcc, string acc) => context.ReadAction((r) => { r.Config = r.Config[r.Param?.ToString()]; });

    internal static void LookupRead(Parser parser, ParsingContext context, int intAcc, string acc) => context.ReadTransform((f) => {
        var c = f.Config[f.Read.ToString()];
        return new ParsingFocusRead
        {
            Config = c,
            Key = f.Key,
            Read = f.Read,
        };
    });

    internal static void LookupReadValue(Parser parser, ParsingContext context, int intAcc, string acc) => context.WriteFromRead(x => x.Config.Value, (m, f, r) => { m.Append(f.Write, r); });
}

internal struct OperationMetaData
{
    internal WriteType WriteType;
    internal string Name { get; private set; }
    internal OperationMetaData(WriteType wt, string name)
    {
        WriteType = wt;
        Name = name;
    }
}