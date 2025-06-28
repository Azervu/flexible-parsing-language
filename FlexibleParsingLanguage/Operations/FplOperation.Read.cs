using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Operations;

internal static partial class FplOperation {

    internal static readonly OpConfig Read = new OpConfig(".", OpSequenceType.RightInput | OpSequenceType.LeftInput | OpSequenceType.Default, (p, op) => CompileAccessorOperation(p, op, OperationRead, OperationReadInt, OperationReadDynamic));

    internal static void OperationRead(FplQuery parser, ParsingContext context, ParseOperationData d)
    {
#if DEBUG
        if (d.StringAcc == null)
            throw new Exception("OperationRead null access");
#endif
        context.ReadFunc(d.Id, (m, readSrc) => m.Parse(readSrc, d.StringAcc));
    }

    internal static void OperationReadInt(FplQuery parser, ParsingContext context, ParseOperationData d)
    {
        context.ReadFunc(d.Id, (m, readSrc) => m.Parse(readSrc, d.IntAcc));
    }

    internal static void OperationReadDynamic(FplQuery parser, ParsingContext context, ParsingFocus focus, ParseOperationData d)
    {

        var ww = context.Focus.Writes[context.Focus.Active.WriteId];
        var rr = context.Focus.Reads[focus.ReadId];
        var intersections = context.Focus.GenerateSequencesIntersection(
            ww, ww.Select(x => x.SequenceId).ToList(),
            rr, rr.Select(x => x.SequenceId).ToList()
        );

        foreach (var x in intersections)
        {
            foreach (var r in x.AVal.Foci)
            {
                context.ReadFunc(d.Id, (m, readSrc) =>
                {
                    switch (r.Value.V)
                    {
                        case byte i:
                            return m.Parse(readSrc, i);
                        case long i:
                            return m.Parse(readSrc, (int)i);
                        case int i:
                            return m.Parse(readSrc, i);
                        case string s:
                            return m.Parse(readSrc, s);
                        default:
                            return m.Parse(readSrc, r.Value.V.ToString());
                    }
                });
            }
        }
    }




}