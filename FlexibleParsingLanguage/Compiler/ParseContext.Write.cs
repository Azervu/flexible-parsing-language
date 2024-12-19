﻿using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler;

internal partial class ParseContext
{
    internal WriteType ProcessWrite(ParseData parser, bool finalContextOp)
    {
        if (ChildOperator == null || ChildOperator.Count == 0)
        {
            Action<Parser, ParsingContext, int, string> opt = finalContextOp ? ParseOperationType.WriteFromRead : ParseOperationType.Write;



            HandleOp(parser, new ParseOperation(opt, Param));
            return WriteType.Object;
        }

        var op = ChildOperator[0].Operator;

        switch (op)
        {
            case "*":
                HandleOp(parser, new ParseOperation(ParseOperationType.WriteFlatten));
                break;
            default:
                throw new Exception($"Unsupported param operator | op = {op}");
        }


        return WriteType.Array;
    }
}


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