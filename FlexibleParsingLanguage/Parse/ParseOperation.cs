namespace FlexibleParsingLanguage.Parse;

internal class ParseOperation
{
    internal ParsesOperationType OpType { get; set; }
    internal Action<FplQuery, ParsingContext, int, string> Op { get => OpType.Op; }
    internal string StringAcc { get; set; }
    internal int IntAcc { get; set; }

    internal ParseOperation(Action<FplQuery, ParsingContext, int, string> op, string acc = null)
    {
        OpType = new ParsesOperationType(op);
        StringAcc = acc;
        IntAcc = -1;
    }

    internal ParseOperation(Action<FplQuery, ParsingContext, int, string> op, int acc)
    {
        OpType = new ParsesOperationType(op);
        IntAcc = acc;
    }
}