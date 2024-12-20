using FlexibleParsingLanguage.Parse;

namespace FlexibleParsingLanguage.Compiler;

internal partial class ParseContext
{
    internal List<ParseOperation> ProcessParam()
    {
        if (ChildOperator == null)
            return [new ParseOperation(ParsesOperationType.ParamLiteral, Token.Acc),];

        foreach (var accessor in ChildOperator) {
            switch (accessor.Operator)
            {
                case "@":
                    return [
                        new ParseOperation(ParsesOperationType.Read, accessor.Token.Acc),
                        new ParseOperation(ParsesOperationType.ParamFromRead),
                    ];
            }
        }

        return [];

        /*

        HandleOps(parser, [
            new ParseOperation(ParseOperationType.ParamLiteral, Token.Acc),
            new ParseOperation(ParseOperationType.Lookup),
            new ParseOperation(ParseOperationType.ParamToRead),
        ]);
        */
    }
}
