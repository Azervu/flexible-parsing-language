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

    internal static void OperationRead(FplQuery parser, ParsingContext context, int intAcc, string acc)
    {
#if DEBUG
        if (acc == null)
            throw new Exception("OperationRead null access");
#endif
        context.ReadFunc((m, readSrc) => m.Parse(readSrc, acc));
    }

    internal static void OperationReadInt(FplQuery parser, ParsingContext context, int intAcc, string acc)
    {
        context.ReadFunc((m, readSrc) => m.Parse(readSrc, intAcc));
    }




    internal static void OperationReadDynamic(FplQuery parser, ParsingContext context, ParsingFocus focus)
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
                context.ReadFunc((m, readSrc) =>
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