using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FlexibleParsingLanguage;
internal partial class Lexicalizer
{
    private void ProcessWriteOperator(int i, ParserConfig config, ParseData parser, ParseContext ctx, AccessorData acc)
    {
        ctx.WriteMode = WriteMode.Written;
        var key = new OperatorKey(ctx.ActiveId, acc.Operator, acc.Accessor, true);

        if (parser.OpsMap.TryGetValue(key, out var writeId))
        {
            ctx.ActiveId = writeId;
            return;
        }

        EnsureWriteOpLoaded(config, parser, ctx, acc);

        ctx.ActiveId = ++parser.IdCounter;
        parser.SaveOps.Add(ctx.ActiveId);
        parser.LoadedId = ctx.ActiveId;
        parser.OpsMap.Add(key, ctx.ActiveId);


        var nextAcc = NextReadOperator(i, ctx);
        var nextIsArray = nextAcc?.Numeric != false;

        if (acc.Numeric)
        {
            if (!int.TryParse(acc.Accessor, out var intAcc))
                throw new ArgumentException("Invalid Query | accessor not int");

            if (nextIsArray)
                parser.Ops.Add(new ParseOperation(ParseOperationType.WriteArrayInt, intAcc));
            else
                parser.Ops.Add(new ParseOperation(ParseOperationType.WriteInt, intAcc));
        }
        else
        {
            if (nextIsArray)
                parser.Ops.Add(new ParseOperation(ParseOperationType.WriteArray, acc.Accessor));
            else
                parser.Ops.Add(new ParseOperation(ParseOperationType.Write, acc.Accessor));

        }
        parser.Ops.Add(new ParseOperation(ParseOperationType.Save, ctx.ActiveId));

    }

    private void ProcessWriteFlattenOperator(int i, ParserConfig config, ParseData parser, ParseContext ctx, AccessorData acc)
    {
        //parser.Ops.Add(new ParseOperation(ParseOperationType.ReadFlatten));
        var nextAcc = NextReadOperator(i, ctx);
        parser.Ops.Add(new ParseOperation(acc.Numeric ? ParseOperationType.WriteFlattenObj : ParseOperationType.WriteFlattenArray));
        //var nextWriteOp = 
        //parser.Ops.Add(new ParseOperation(ParseOperationType.WriteFlatten));
    }

    private AccessorData NextReadOperator(int i, ParseContext ctx)
    {
        for (var j = i + 1; j < ctx.Accessors.Count; j++)
        {
            var op = ctx.Accessors[j];

            if (op.Ctx != null)
                return FirstRead(op.Ctx);

            return op;
        }
        return null;
    }

    private AccessorData FirstRead(ParseContext ctx)
    {
        var writes = false;
        foreach (var op in ctx.Accessors)
        {
            if (op.Ctx != null)
                return FirstRead(op.Ctx);
            if (writes)
                return op;
            if (op.Operator == ':')
                writes = true;
 
        }
        return null;
    }



}
