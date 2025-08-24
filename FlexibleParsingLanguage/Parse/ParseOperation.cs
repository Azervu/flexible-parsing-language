using FlexibleParsingLanguage.Compiler;

namespace FlexibleParsingLanguage.Parse;

internal struct ParseOperationData
{
    internal int Id { get; set; }
    internal string StringAcc { get; set; }
    internal int IntAcc { get; set; }
}

internal class ParseOperation
{
    internal RawOp Metadata { get; set; }

    internal ParsesOperationType OpType { get; set; }
    internal Action<FplQuery, ParsingContext, ParseOperationData> Op { get => OpType.Op; }
    internal ParseOperationData Data { get; set; }
    internal ParseOperation(RawOp o, Action<FplQuery, ParsingContext, ParseOperationData> op, string acc = null)
    {
        OpType = new ParsesOperationType(op);
        Data = new ParseOperationData
        {
            Id = o.Id,
            StringAcc = acc,
            IntAcc = -1
        };
    }

    internal ParseOperation(RawOp o, Action<FplQuery, ParsingContext, ParseOperationData> op, int acc)
    {
        OpType = new ParsesOperationType(op);
        Data = new ParseOperationData
        {
            Id = o.Id,
            IntAcc = acc
        };
    }
}