using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler;

internal partial class Compiler
{
    private IEnumerable<ParseOperation> ProcessLookupOperation(ParserConfig config, ParseData parser, ParseContext ctx, ParseContext acc)
    {
        var next = ctx.NextChild;
        ctx.Index++;

        foreach (var o in ProcessOperation(config, parser, ctx, next))
            yield return o;

        yield return new ParseOperation(ParseOperationType.LookupRead);

        if (ctx.LastWriteOp)
            yield return new ParseOperation(ParseOperationType.LookupReadValue);
    }
}