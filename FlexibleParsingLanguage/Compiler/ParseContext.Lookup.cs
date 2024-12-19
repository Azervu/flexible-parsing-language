using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler;

internal partial class ParseContext
{
    internal WriteType? ProcessLookup(ParseData parser)
    {
        var ops = ProcessParam();
        ops.Add(new ParseOperation(ParseOperationType.Lookup));
        ops.Add(new ParseOperation(ParseOperationType.ParamToRead));
        HandleOps(parser, ops.ToArray());
        return null;
    }

    internal WriteType? ProcessContextLookup(ParseData parser)
    {
        var ops = ProcessParam();
        ops.Add(new ParseOperation(ParseOperationType.ChangeLookup));
        HandleOps(parser, ops.ToArray());
        return null;
    }
}
