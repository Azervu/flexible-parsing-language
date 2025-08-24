using FlexibleParsingLanguage.Compiler;
using System.Security.Cryptography;

namespace FlexibleParsingLanguage.Parse;

internal partial struct ParsesOperationType
{
    internal Action<FplQuery, ParsingContext, ParseOperationData> Op { get; private set; }

    internal ParsesOperationType(Action<FplQuery, ParsingContext, ParseOperationData> op)
    {
        if (op == null)
            throw new ArgumentNullException(nameof(op));
        Op = op;
    }

    internal static void Write(FplQuery parser, ParsingContext context, ParseOperationData d) => context.WriteAction(d.Id, (m, writeHeader) =>
    {
        var w = m.BlankMap();
        m.Write(writeHeader.V, d.StringAcc, w);
        return new ValueWrapper(w);
    });

    internal static void WriteArray(FplQuery parser, ParsingContext context, ParseOperationData d) => context.WriteAction(d.Id, (m, writeHeader) =>
    {
        var w2 = m.BlankArray();
        m.Write(writeHeader.V, d.StringAcc, w2);
        return new ValueWrapper(w2);
    });


    internal static void Save(FplQuery parser, ParsingContext context, ParseOperationData op) => context.Focus.Save(op.Id);
    internal static void Load(FplQuery parser, ParsingContext context, ParseOperationData op) => context.Focus.Load(op.IntAcc);
    internal static void ReadName(FplQuery parser, ParsingContext context, ParseOperationData op) => context.Focus.ReadInner(op.Id, (focus) => new FocusEntry
    {
        Key = focus.Key,
        Value = focus.Key,
        SequenceId = focus.SequenceId
    });
    internal static void WriteFromRead(FplQuery parser, ParsingContext context, ParseOperationData d) => context.WriteStringFromRead(d.StringAcc);
    internal static void WriteFlatten(FplQuery parser, ParsingContext context, ParseOperationData d)
    {
        switch (d.IntAcc)
        {
            case 1:
                context.WriteFlatten(d.Id);
                break;
            case 2:
                context.WriteFlattenArray(d.Id);
                break;
        }
    }
}