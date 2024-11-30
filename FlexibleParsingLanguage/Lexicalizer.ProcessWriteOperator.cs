namespace FlexibleParsingLanguage;
internal partial class Lexicalizer
{
    private void ProcessWriteOperator(ParserConfig config, ParseData parser, ParseContext ctx, AccessorData acc)
    {
        ctx.WriteMode = WriteMode.Written;
        var key = new OperatorKey(ctx.ActiveId, acc.Operator, acc.Accessor, true);

        if (parser.OpsMap.TryGetValue(key, out var writeId))
        {
            ctx.ActiveId = writeId;
            return;
        }

        if (config.WriteArrayRoot == null)
            config.WriteArrayRoot = acc == null || acc.Numeric || ctx.WriteMode == WriteMode.Read;

        EnsureWriteOpLoaded(config, parser, ctx, acc);

        ctx.ActiveId = ++parser.IdCounter;
        parser.SaveOps.Add(ctx.ActiveId);
        parser.LoadedId = ctx.ActiveId;
        parser.OpsMap.Add(key, ctx.ActiveId);

        if (acc.Numeric)
        {
            if (!int.TryParse(acc.Accessor, out var intAcc))
                throw new ArgumentException("");
            parser.Ops.Add(new ParseOperation(ParseOperationType.WriteAccessInt, intAcc));
        }
        else
        {
            
            parser.Ops.Add(new ParseOperation(ParseOperationType.WriteAccess, acc.Accessor));
        }
        parser.Ops.Add(new ParseOperation(ParseOperationType.Save, ctx.ActiveId));
    }
}
