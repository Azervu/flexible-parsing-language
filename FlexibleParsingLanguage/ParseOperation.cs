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

        var s = 456654;

        switch (OpType)
        {
            case ParseOperationType.Save:
                ctx.Store[IntAcc] = ctx.Focus;
                break;
            case ParseOperationType.Load:
                ctx.Focus = ctx.Store[IntAcc];
                break;
            case ParseOperationType.Root:
                ctx.ToRoot();
                break;
            case ParseOperationType.WriteAccessInt:
                var w1 = ctx.WritingModule.BlankArray();
                ctx.WriteAction((m, writeHeader) => {
                    m.Write(writeHeader, IntAcc, w1);
                    return w1;
                });
                break;
            case ParseOperationType.WriteAccess:


                var w2 = ctx.WritingModule.BlankMap();
                ctx.WriteAction((m, writeHeader) => {
                    m.Write(writeHeader, StringAcc, w2);
                    return w2;
                });



                break;

            case ParseOperationType.WriteAddRead:
                ctx.WriteAddRead((m, r, w) => m.Append(w, r));
                break;
            case ParseOperationType.WriteFromRead:
                ctx.WriteFromRead((m, r, w) => m.Write(w, StringAcc, r));
                break;
            case ParseOperationType.ReadAccess:
                ctx.ReadAction((m, readSrc) => m.Parse(readSrc, StringAcc));
                break;
            case ParseOperationType.ReadAccessInt:
                ctx.ReadAction((m, readSrc) => m.Parse(readSrc, IntAcc));
                break;
            case ParseOperationType.ReadForeach:
                ctx.ReadFlatten();
                break;
        }
    }

}
