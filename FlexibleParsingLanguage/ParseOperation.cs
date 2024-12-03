using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage;

internal class ParseOperation
{
    internal ParseOperationType OpType { get; set; }
    internal string StringAcc { get; set; }
    internal int IntAcc { get; set; }


    internal ParseOperation(ParseOperationType opType, string acc = null)
    {
        this.OpType = opType;
        this.StringAcc = acc;
        this.IntAcc = -1;
    }

    internal ParseOperation(ParseOperationType opType, int acc)
    {
        this.OpType = opType;
        this.IntAcc = acc;
    }

    internal void AppyOperation(ParsingContext ctx)
    {
#if DEBUG
        var s = 456654;
#endif
        switch (OpType)
        {
            case ParseOperationType.ReadRoot:
                ctx.ToRootRead();
                break;
            case ParseOperationType.Save:
                ctx.Store[IntAcc] = ctx.Focus;
                break;
            case ParseOperationType.Load:
                ctx.Focus = ctx.Store[IntAcc];
                break;

            case ParseOperationType.Read:
                ctx.ReadAction((m, readSrc) => m.Parse(readSrc, StringAcc));
                break;
            case ParseOperationType.ReadInt:
                ctx.ReadAction((m, readSrc) => m.Parse(readSrc, IntAcc));
                break;
            case ParseOperationType.ReadFlatten:
                ctx.ReadFlatten();
                break;

            case ParseOperationType.ReadName:
                ctx.ReadName();
                break;
            case ParseOperationType.WriteInt:

                ctx.WriteAction((m, writeHeader) =>
                {
                    var w = ctx.WritingModule.BlankMap();
                    m.Write(writeHeader, IntAcc, w);
                    return w;
                });
                break;
            case ParseOperationType.Write:

                ctx.WriteAction((m, writeHeader) =>
                {
                    var w = m.BlankMap();
                    m.Write(writeHeader, StringAcc, w);
                    return w;
                });
                break;

            case ParseOperationType.WriteArray:

                ctx.WriteAction((m, writeHeader) =>
                {
                    var w2 = m.BlankArray();
                    m.Write(writeHeader, StringAcc, w2);
                    return w2;
                });
                break;

            case ParseOperationType.WriteArrayInt:

                ctx.WriteAction((m, writeHeader) =>
                {
                    var w1 = m.BlankArray();
                    m.Write(writeHeader, IntAcc, w1);
                    return w1;
                });
                break;

            case ParseOperationType.WriteAddRead:
                ctx.WriteAddRead();
                break;
            case ParseOperationType.WriteFromRead:
                ctx.WriteFromRead(StringAcc);
                break;
            case ParseOperationType.WriteFlattenArray:
                ctx.WriteFlattenArray();
                break;
            case ParseOperationType.WriteFlattenObj:
                ctx.WriteFlatten();
                break;
            case ParseOperationType.WriteNameFromRead:

                break;
        }
    }
}