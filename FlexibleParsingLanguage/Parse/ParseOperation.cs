using FlexibleParsingLanguage.Compiler;
using System.Diagnostics;

namespace FlexibleParsingLanguage.Parse;

internal struct ParseOperationData
{
#if DEBUG
    internal RawOp Debug;
#endif


    internal int Id { get; set; }
    internal string StringAcc { get; set; }
    internal int IntAcc { get; set; }

    internal int ReadId { get; set; }
    internal int WriteId { get; set; }
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

    internal ParseOperation(RawOp o, Action<FplQuery, ParsingContext, ParseOperationData> op, ParseOperationData data)
    {
#if DEBUG
        data.Debug = o;
#endif


        data.Id = o.Id;
        OpType = new ParsesOperationType(op);
        Data = data;
    }

}