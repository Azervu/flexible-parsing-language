using FlexibleParsingLanguage.Parse;

namespace FlexibleParsingLanguage.Compiler;
internal partial class Compiler
{
    private ParseOperation? ProcessWriteOperator(ParserConfig config, ParseData parser, ParseContext ctx, ParseContext acc)
    {
        ctx.WriteMode = WriteMode.Written;

        var nextAcc = ctx.NextReadOperator();
        var nextIsArray = nextAcc?.Numeric == true;

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
}