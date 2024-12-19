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
        if (Accessors == null)
        {
            HandleOps(parser, [
                new ParseOperation(ParseOperationType.ParamLiteral, Token.Acc),
                new ParseOperation(ParseOperationType.Lookup),
                new ParseOperation(ParseOperationType.ParamToRead),
            ]);
        }





        HandleOps(parser, [
            new ParseOperation(ParseOperationType.ParamLiteral, Token.Acc),
            new ParseOperation(ParseOperationType.Lookup),
            new ParseOperation(ParseOperationType.ParamToRead),
        ]);


        return null;
    }
}
