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


    internal static void Write(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.WriteAction((m, writeHeader) =>
    {
        var w = m.BlankMap();
        m.Write(writeHeader.V, acc, w);
        return new ValueWrapper(w);
    });

    internal static void WriteArray(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.WriteAction((m, writeHeader) =>
    {
        var w2 = m.BlankArray();
        m.Write(writeHeader.V, acc, w2);
        return new ValueWrapper(w2);
    });

    internal static void WriteRoot(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.Focus.LoadWrite(Compiler.Compiler.RootId);
    internal static void Save(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.Focus.Save(intAcc);
    internal static void Load(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.Focus.Load(intAcc);
    internal static void ReadName(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.ReadName();

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
}