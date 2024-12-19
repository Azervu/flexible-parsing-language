using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler
{
    internal partial class ParseContext
    {
        internal void ProcessParam(ParseData parser)
        {
            if (Accessors == null)
            {
                HandleOps(parser, [new ParseOperation(ParseOperationType.ParamLiteral, Token.Acc),]);
                return;
            }





            foreach (var accessor in Accessors) {
            
                
                switch (accessor.Operator)
                {
                    case "@":
                        HandleOps(parser, [
                            new ParseOperation(ParseOperationType.ParamFromRead),
                        ]);
                        break;
                }

            }



            /*

            HandleOps(parser, [
                new ParseOperation(ParseOperationType.ParamLiteral, Token.Acc),
                new ParseOperation(ParseOperationType.Lookup),
                new ParseOperation(ParseOperationType.ParamToRead),
            ]);
            */
        }
    }
}
