using FlexibleParsingLanguage.Parse;

namespace FlexibleParsingLanguage.Compiler;
internal partial class Compiler
{
    private ParseOperation? ProcessWriteOperator(ParserRootConfig config, ParseData parser, ParseContext ctx, ParseContext acc)
    {
        var nextAcc = ctx.NextReadOperator();
        var nextIsArray = nextAcc?.Numeric == true;

        if (acc.Numeric)
        {
            if (!int.TryParse(acc.Param, out var intAcc))
                throw new ArgumentException("Invalid Query | accessor not int");

            if (nextIsArray)
                return new ParseOperation(ParseOperationType.WriteArrayInt, intAcc);
            else
                return new ParseOperation(ParseOperationType.WriteInt, intAcc);
        }
        else
        {
            if (nextIsArray)
                return new ParseOperation(ParseOperationType.WriteArray, acc.Param);
            else
                return new ParseOperation(ParseOperationType.Write, acc.Param);

        }
    }
}