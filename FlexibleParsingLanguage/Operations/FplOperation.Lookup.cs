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

    internal static readonly OpConfig Lookup = new OpConfig("#", OpSequenceType.RightInput | OpSequenceType.LeftInput, (p, op) => CompileAccessorOperation(p, op, OperationLookup, null, OperationLookupDynamic));

    internal static readonly OpConfig ChangeLookupContext = new OpConfig("##", OpSequenceType.RightInput | OpSequenceType.LeftInput, (p, op) => CompileAccessorOperation(p, op, OperationLookupChange, null, OperationLookupChangeDynamic));

    internal static void OperationLookup(FplQuery parser, ParsingContext context, int intAcc, string acc)
    {
        context.Focus.NextRead(context.Focus.GenerateSequencesIntersectionReadConfig().Select(r =>
        {

#if DEBUG
            if (r.AVal.Foci.Count != 1)
                throw new Exception("TODO handle multi read per config");
#endif


            var read = r.AVal.Foci[0];

            return new FocusEntry
            {
                Key = new ValueWrapper(acc),
                Value = new ValueWrapper(read.Config.Entries[acc].Value),
                SequenceId = read.SequenceId,
            };
        }).ToList());
    }

    internal static void OperationLookupInt(FplQuery parser, ParsingContext context, int intAcc, string acc)
    {
        context.Focus.NextRead(context.Focus.GenerateSequencesIntersectionReadConfig().Select(r =>
        {

#if DEBUG
            if (r.AVal.Foci.Count != 1)
                throw new Exception("TODO handle multi read per config");
#endif


            var read = r.AVal.Foci[0];
            var a = intAcc.ToString();
            return new FocusEntry
            {
                Key = new ValueWrapper(a),
                Value = new ValueWrapper(read.Config.Entries[a].Value),
                SequenceId = read.SequenceId,
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

            var v = c.Config.Entries[acc].Value;

            return new FocusEntry
            {
                SequenceId = x.Primary.SequenceId,
                Key = new ValueWrapper(acc),
                Value = new ValueWrapper(v),
            };
        }).ToList());
    }





    internal static void OperationLookupChange(FplQuery parser, ParsingContext context, int intAcc, string acc)
    {
        context.Focus.NextConfig(
            context.Focus
            .Configs[context.Focus.Active.ConfigId]
            .Select(r =>
            {
                if (!r.Config.Entries.TryGetValue(acc, out var c))
                    c = r.Config;

                return new ConfigEntry(c, r.SequenceId);
            }).ToList()
        );
    }

    internal static void OperationLookupChangeDynamic(FplQuery parser, ParsingContext context, ParsingFocus focus)
    {

        var config = context.Focus.Configs[context.Focus.Active.ConfigId];
        var read = context.Focus.Reads[focus.ReadId];
        var intersections = context.Focus.GenerateSequencesIntersection(
            read, read.Select(x => x.SequenceId).ToList(),
            config, config.Select(x => x.SequenceId).ToList()
        );


        context.Focus.NextConfig(intersections.Select(x =>
        {
            var acc = x.Primary.Value.V.ToString();
            var c = x.AVal.Foci[0].Config;

            if (c.Entries.TryGetValue(acc, out var c2))
                c = c2;

            return new ConfigEntry(c, x.Primary.SequenceId); ;
        }).ToList());
    }






}




