namespace FlexibleParsingLanguage;
internal partial class Lexicalizer
{
    private void ProcessWriteOperator(ParseData data, ParseContext ctx, AccessorData acc)
    {
        ctx.WriteMode = WriteMode.Written;
        var key = new OperatorKey(ctx.WriteId, acc.Operator, acc.Accessor, true);

        if (data.OpsMap.TryGetValue(key, out var writeId))
        {
            ctx.WriteId = writeId;
            return;
        }

        EnsureWriteOpLoaded(data, ctx, acc);

        ctx.WriteId = ++data.IdCounter;
        data.LoadedWriteId = ctx.WriteId;
        data.OpsMap.Add(key, ctx.WriteId);

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
