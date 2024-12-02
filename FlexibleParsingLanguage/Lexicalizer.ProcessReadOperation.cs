namespace FlexibleParsingLanguage;
internal partial class Lexicalizer
{
    private void ProcessReadOperator(ParseData parser, ParseContext ctx, AccessorData data)
    {
        var key = new OperatorKey(ctx.ActiveId, data.Operator, data.Accessor, false);
        if (parser.OpsMap.TryGetValue(key, out var readId))
        {
            ctx.ActiveId = readId;
            return;
        }

        EnsureReadOpLoaded(parser, ctx);
        ctx.ActiveId = ++parser.IdCounter;
        parser.SaveOps.Add(ctx.ActiveId);
        parser.LoadedId = ctx.ActiveId;
        parser.OpsMap.Add(key, ctx.ActiveId);
        parser.Ops.Add(new ParseOperation(ParseOperationType.Read, data.Accessor));
        parser.Ops.Add(new ParseOperation(ParseOperationType.Save, ctx.ActiveId));
    }

    private void ProcessReadFlattenOperator(ParseData parser, ParseContext ctx, AccessorData data)
    {
        var opId = ++parser.IdCounter;
        var op = new ParseOperation(ParseOperationType.ReadFlatten, data.Accessor);

        var key = new OperatorKey(ctx.ActiveId, data.Operator, data.Accessor, false);
        if (parser.OpsMap.TryGetValue(key, out var readId))
        {
            ctx.ActiveId = readId;
            return;
        }

        EnsureReadOpLoaded(parser, ctx);
        ctx.ActiveId = opId;
        parser.SaveOps.Add(ctx.ActiveId);
        parser.LoadedId = ctx.ActiveId;
        parser.OpsMap.Add(key, ctx.ActiveId);
        parser.Ops.Add(op);
        parser.Ops.Add(new ParseOperation(ParseOperationType.Save, ctx.ActiveId));
    }
}