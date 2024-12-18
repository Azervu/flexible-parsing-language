using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler;

internal partial class Compiler
{
    IEnumerable<ParseOperation> ProcessOperation(ParserConfig config, ParseData parser, ParseContext ctx, ParseContext accessor)
    {
        switch (accessor.Operator)
        {
            case "|":
                if (ctx.WriteMode == WriteMode.Read)
                    yield return new ParseOperation(ParseOperationType.TransformRead, accessor.Accessor);
                else
                    yield return new ParseOperation(ParseOperationType.TransformWrite, accessor.Accessor);
                break;
            case "$":
                if (ctx.WriteMode == WriteMode.Read)
                    yield return new ParseOperation(ParseOperationType.ReadRoot);
                else
                    yield return new ParseOperation(ParseOperationType.WriteRoot);
                break;
            case ":":
                ctx.WriteMode = WriteMode.Write;
                break;
            case "€":
                foreach (var op in ProcessLookupOperation(config, parser, ctx, accessor))
                    yield return op;
                break;
            case "*":
                if (ctx.WriteMode == WriteMode.Read)
                {
                    yield return new ParseOperation(ParseOperationType.ReadFlatten, accessor.Accessor);
                }
                else
                {
                    var nextOp = ctx.NextReadOperator();
                    var nextNumeric = nextOp?.Numeric ?? true;
                    yield return new ParseOperation(nextNumeric ? ParseOperationType.WriteFlattenArray : ParseOperationType.WriteFlattenObj);
                }
                break;
            case "~":
                if (ctx.WriteMode == WriteMode.Read)
                    yield return new ParseOperation(ParseOperationType.ReadName);
                else
                    yield return new ParseOperation(ParseOperationType.WriteNameFromRead);
                break;
            case ".":
            case "\"":
            case "'":
            case "[":
                if (ctx.WriteMode == WriteMode.Read)
                {
                    yield return new ParseOperation(ParseOperationType.Read, accessor.Accessor);
                }
                else if (ctx.LastWriteOp)
                {
                    ctx.ProcessedEnd = true;
                    yield return ProcessContextEndingOperator(config, parser, ctx, accessor);
                }
                else
                {
                    yield return ProcessWriteOperator(config, parser, ctx, accessor);
                }
                break;
            default:
                break;
        }
    }


}
