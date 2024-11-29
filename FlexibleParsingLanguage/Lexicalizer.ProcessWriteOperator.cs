﻿namespace FlexibleParsingLanguage;
internal partial class Lexicalizer
{
    private void ProcessWriteOperator(ParserConfig config, ParseData data, ParseContext ctx, AccessorData acc)
    {
        ctx.WriteMode = WriteMode.Written;
        var key = new OperatorKey(ctx.ActiveId, acc.Operator, acc.Accessor, true);

        if (data.OpsMap.TryGetValue(key, out var writeId))
        {
            ctx.ActiveId = writeId;
            return;
        }

        EnsureWriteOpLoaded(config, data, ctx, acc);

        ctx.ActiveId = ++data.IdCounter;
        data.LoadedId = ctx.ActiveId;
        data.OpsMap.Add(key, ctx.ActiveId);

        if (acc.Numeric)
        {
            if (!int.TryParse(acc.Accessor, out var id))
                throw new ArgumentException("");
            data.Ops.Add(new ParseOperation(ParseOperationType.WriteAccess, acc.Accessor));
        }
        else
        {
            data.Ops.Add(new ParseOperation(ParseOperationType.WriteAccess, acc.Accessor));
        }
    }
}
