namespace FlexibleParsingLanguage;
internal partial class Lexicalizer
{
    private void ProcessReadOperator(ParseData parser, ParseContext ctx, AccessorData data)
    {
        var key = new OperatorKey(ctx.ReadId, data.Operator, data.Accessor, false);
        if (parser.OpsMap.TryGetValue(key, out var readId))
        {
            ctx.ReadId = readId;
            return;
        }

        EnsureReadOpLoaded(parser, ctx);
        ctx.ReadId = ++parser.IdCounter;
        parser.SaveOps.Add(ctx.ReadId);
        parser.LoadedReadId = ctx.ReadId;
        parser.OpsMap.Add(key, ctx.ReadId);
        parser.Ops.Add(new ParseOperation(ParseOperationType.ReadAccess, data.Accessor));
        parser.Ops.Add(new ParseOperation(ParseOperationType.ReadSave, ctx.ReadId));
    }
}