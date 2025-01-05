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
        context.Focus.NextRead(context.Focus.GenerateSequencesIntersectionReadConfig().Select(r =>
        {
            return new FocusEntry
            {
                Key = new ValueWrapper(acc),
                Value = new ValueWrapper(r.AVal.Foci[0].Config.Entries[acc]),
            };
        }).ToList());
    }

    internal static void OperationLookupDynamic(FplQuery parser, ParsingContext context, ParsingFocus focus)
    {

        var config = context.Focus.Configs[context.Focus.Active.ConfigId];
        var read = context.Focus.Reads[focus.ReadId];
        var intersections = context.Focus.GenerateSequencesIntersection(
            read, read.Select(x => x.SequenceId).ToList(),
            config, config.Select(x => x.SequenceId).ToList()
        );

        context.Focus.NextRead(intersections.Select(x =>
        {
            var acc = x.Primary.Value.V.ToString();

            var c = x.AVal.Foci[0];

            return new FocusEntry
            {
                Key = new ValueWrapper(acc),
                Value = new ValueWrapper(c.Config.Entries[acc]),
            };
        }).ToList());
    }
}




