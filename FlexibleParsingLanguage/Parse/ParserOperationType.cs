 using FlexibleParsingLanguage.Compiler;
using System;
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

    internal static void Save(FplQuery parser, ParsingContext context, ParseOperationData op)
    {
        context.Focus.Store[op.Id] = context.Focus.Active;
    }

    internal static void ReadName(FplQuery parser, ParsingContext context, ParseOperationData op) => context.Focus.ReadInner(op.Id, (focus) => new FocusEntry
    {
        Key = focus.Key,
        Value = focus.Key,
        SequenceId = focus.SequenceId
    });
}