using FlexibleParsingLanguage.Parse;

namespace FlexibleParsingLanguage.Compiler;
internal partial class Lexicalizer
{
    private ParseOperation? ProcessWriteOperator(int i, ParserConfig config, ParseData parser, ParseContext ctx, AccessorData acc)
    {
        ctx.WriteMode = WriteMode.Written;

        var nextAcc = NextReadOperator(i, ctx);
        var nextIsArray = nextAcc?.Numeric != false;

        if (acc.Numeric)
        {
            if (!int.TryParse(acc.Accessor, out var intAcc))
                throw new ArgumentException("Invalid Query | accessor not int");

            if (nextIsArray)
                return new ParseOperation(ParseOperationType.WriteArrayInt, intAcc);
            else
                return new ParseOperation(ParseOperationType.WriteInt, intAcc);
        }
        else
        {
            if (nextIsArray)
                return new ParseOperation(ParseOperationType.WriteArray, acc.Accessor);
            else
                return new ParseOperation(ParseOperationType.Write, acc.Accessor);

        }
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
