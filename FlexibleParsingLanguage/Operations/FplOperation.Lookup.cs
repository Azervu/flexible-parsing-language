using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Operations;

internal static partial class FplOperation
{

    internal static readonly OpConfig Lookup = new OpConfig("#", OpSequenceType.RightInput | OpSequenceType.LeftInput, (p, op) => CompileAccessorOperation(p, op, OperationLookup, OperationLookupDynamic));

    //internal static readonly OpConfig ChangeLookupContext = new OpConfig("##", OpSequenceType.RightInput | OpSequenceType.LeftInput, CompileLookup);

    internal static void OperationLookup(FplQuery parser, ParsingContext context, int intAcc, string acc)
    {
#if DEBUG
        if (acc == null)
            throw new Exception("OperationRead null access");
#endif

        //context.Focus.Active.

        context.ReadFunc((m, readSrc) => m.Parse(readSrc, acc));
    }

    internal static void OperationLookupDynamic(FplQuery parser, ParsingContext context, ParsingFocus focus)
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
                context.ReadFunc((m, readSrc) => m.Parse(readSrc, r.Value.V.ToString()));
            }
        }
    }
}




