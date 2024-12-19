using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Parse;

internal class ParseOperation
{
    internal ParseOperationType OpType { get; set; }
    internal string StringAcc { get; set; }
    internal int IntAcc { get; set; }


    internal ParseOperation(ParseOperationType opType, string acc = null)
    {
        OpType = opType;
        StringAcc = acc;
        IntAcc = -1;
    }

    internal ParseOperation(ParseOperationType opType, int acc)
    {
        OpType = opType;
        IntAcc = acc;
    }

    internal void AppyOperation(Parser parser, ParsingContext ctx)
    {
        switch (OpType)
        {
            case ParseOperationType.ReadRoot:
                ctx.ToRootRead();
                break;
            case ParseOperationType.WriteRoot:
                ctx.ToRootWrite();
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
                ctx.WriteStringFromRead(StringAcc);
                break;
            case ParseOperationType.WriteFlatten:
                switch (IntAcc)
                {
                    case 1:
                        ctx.WriteFlatten();
                        break;
                    case 2:
                        ctx.WriteFlattenArray();
                        break;
                }
                break;
            case ParseOperationType.WriteNameFromRead:

                break;
            case ParseOperationType.Function:
                var transfomer = parser._converter[StringAcc];
                ctx.ReadTransformValue(transfomer.Convert);
                break;
            case ParseOperationType.LookupRead:
                ctx.ReadTransform((f) => {
                    var c = f.Config[f.Read.ToString()];
                    return new ParsingFocusRead
                    {
                        Config = c,
                        Key = f.Key,
                        Read = f.Read,
                    };
                });
                break;
            case ParseOperationType.LookupReadValue:
                ctx.WriteFromRead(
                    x => x.Config.Value,
                    (m, f, r) => {

                        m.Append(f.Write, r);
                    }
                );
                break;
        }
    }
}