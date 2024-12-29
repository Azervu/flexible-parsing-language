using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler;

internal partial class ParseContext
{
    internal OpCompileType ProcessWrite(ParseData parser, bool finalContextOp)
    {
        if (ChildOperator == null || ChildOperator.Count == 0)
        {
            Action<FplQuery, ParsingContext, int, string> opt = finalContextOp ? ParsesOperationType.WriteFromRead : ParsesOperationType.Write;
            HandleOp(parser, new ParseOperation(opt, Token.Accessor));
            return OpCompileType.WriteObject;
        }

        var op = ChildOperator[0].Operator;

        switch (op)
        {
            case "*":
                HandleOp(parser, new ParseOperation(ParsesOperationType.WriteFlatten));
                break;
            case "$":
                HandleOp(parser, new ParseOperation(ParsesOperationType.WriteRoot));
                break;
            default:
                throw new Exception($"Unsupported param operator | op = {op}");
        }


        return OpCompileType.WriteArray;
    }
}


internal partial class OldCompiler
{
    private ParseOperation? ProcessWriteOperator(ParserRootConfig config, ParseData parser, ParseContext ctx, ParseContext acc)
    {
        var nextAcc = ctx.NextReadOperator();
        var nextIsArray = nextAcc?.Numeric == true;

        if (acc.Numeric)
        {
            if (!int.TryParse(acc.Token.Accessor, out var intAcc))
                throw new ArgumentException("Invalid Query | accessor not int");

            if (nextIsArray)
                return new ParseOperation(ParsesOperationType.WriteArrayInt, intAcc);
            else
                return new ParseOperation(ParsesOperationType.WriteInt, intAcc);
        }
        else
        {
            if (nextIsArray)
                return new ParseOperation(ParsesOperationType.WriteArray, acc.Token.Accessor);
            else
                return new ParseOperation(ParsesOperationType.Write, acc.Token.Accessor);

        }
    }
}