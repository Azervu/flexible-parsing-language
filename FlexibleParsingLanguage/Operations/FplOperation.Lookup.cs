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

    internal static readonly OpConfig Lookup = new OpConfig("#", OpSequenceType.RightInput | OpSequenceType.LeftInput, (p, op) => CompileLookup(p, op, OperationLookup, null, OperationLookupDynamic));

    internal static readonly OpConfig ChangeLookupContext = new OpConfig("##", OpSequenceType.RightInput | OpSequenceType.LeftInput, (p, op) => CompileChangeLookup(p, op, OperationLookupChange, null, OperationLookupChangeDynamic));

    private static IEnumerable<ParseOperation> CompileLookup(
        ParseData parser,
        RawOp op,
        Action<FplQuery, ParsingContext, ParseOperationData> accessorAction,
        Action<FplQuery, ParsingContext, ParseOperationData>? intAccessorAction,
        Action<FplQuery, ParsingContext, ParsingNode, ParseOperationData> dynamicAccessorAction
    )
    {
        if (op.Input.Count < 2)
            throw new QueryException(op, $"{op.Input.Count} params | read takes 2+");

        foreach (var x in CompileLoad(parser, op))
            yield return x;

        var input = op.Input[0];
        var accessor = op.Input[1];

        if (accessor.Accessor == null)
            yield return new ParseOperation(op, (q, c, data) => dynamicAccessorAction(q, c, c.Focus.Store[data.IntAcc], data), accessor.Id);
        else if (accessor.Type.SequenceType.All(OpSequenceType.Literal))
            yield return new ParseOperation(op, accessorAction, accessor.Accessor);
        else if (intAccessorAction != null && int.TryParse(accessor.Accessor, out var intAcc))
            yield return new ParseOperation(op, intAccessorAction, intAcc);
        else
            yield return new ParseOperation(op, accessorAction, accessor.Accessor);

        parser.LoadedId[0] = op.Id;

        foreach (var x in CompileSaved(parser, op))
            yield return x;

        parser.ActiveReadId = op.Id;
        op.ReadId = op.Id;
    }

    private static IEnumerable<ParseOperation> CompileChangeLookup(
        ParseData parser,
        RawOp op,
        Action<FplQuery, ParsingContext, ParseOperationData> accessorAction,
        Action<FplQuery, ParsingContext, ParseOperationData>? intAccessorAction,
        Action<FplQuery, ParsingContext, ParsingNode, ParseOperationData> dynamicAccessorAction
    )
    {
        if (op.Input.Count < 2)
            throw new QueryException(op, $"{op.Input.Count} params | read takes 2+");

        foreach (var x in CompileLoad(parser, op))
            yield return x;


        var input = op.Input[0];
        var accessor = op.Input[1];

        if (accessor.Accessor == null)
            yield return new ParseOperation(op, (q, c, data) => dynamicAccessorAction(q, c, c.Focus.Store[data.IntAcc], data), accessor.Id);
        else if (accessor.Type.SequenceType.All(OpSequenceType.Literal))
            yield return new ParseOperation(op, accessorAction, accessor.Accessor);
        else if (intAccessorAction != null && int.TryParse(accessor.Accessor, out var intAcc))
            yield return new ParseOperation(op, intAccessorAction, intAcc);
        else
            yield return new ParseOperation(op, accessorAction, accessor.Accessor);

        parser.LoadedId[0] = op.Id;

        foreach (var x in CompileSaved(parser, op))
            yield return x;

        parser.ActiveReadId = input.ReadId;
        op.ReadId = input.ReadId;
    }





    internal static void OperationLookup(FplQuery parser, ParsingContext context, ParseOperationData d)
    {
        context.Focus.NextRead(d.Id, context.Focus.GenerateSequencesIntersectionReadConfig().Select(r =>
        {

#if DEBUG
            if (r.AVal.Foci.Count != 1)
                throw new Exception("TODO handle multi read per config");
#endif

            var read = r.AVal.Foci[0];
            return new FocusEntry
            {
                Key = new ValueWrapper(d.StringAcc),
                Value = new ValueWrapper(read.Config.Entries[d.StringAcc].Value),
                SequenceId = read.SequenceId,
            };
        }).ToList());
    }

    internal static void OperationLookupInt(FplQuery parser, ParsingContext context, ParseOperationData d)
    {
        context.Focus.NextRead(d.Id, context.Focus.GenerateSequencesIntersectionReadConfig().Select(r =>
        {

#if DEBUG
            if (r.AVal.Foci.Count != 1)
                throw new Exception("TODO handle multi read per config");
#endif


            var read = r.AVal.Foci[0];
            var a = d.IntAcc.ToString();
            return new FocusEntry
            {
                Key = new ValueWrapper(a),
                Value = new ValueWrapper(read.Config.Entries[a].Value),
                SequenceId = read.SequenceId,
            };
        }).ToList());
    }

    internal static void OperationLookupDynamic(FplQuery parser, ParsingContext context, ParsingNode focus, ParseOperationData d)
    {

        var config = context.Focus.Configs[context.Focus.Active.ConfigId];
        var read = context.Focus.Reads[focus.ReadId];
        var intersections = context.Focus.GenerateSequencesIntersection(
            read, read.Select(x => x.SequenceId).ToList(),
            config, config.Select(x => x.SequenceId).ToList()
        );

        context.Focus.NextRead(d.Id, intersections.Select(x =>
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





    internal static void OperationLookupChange(FplQuery parser, ParsingContext context, ParseOperationData d)
    {
        context.Focus.NextConfig(d.Id,
            context.Focus
            .Configs[context.Focus.Active.ConfigId]
            .Select(r =>
            {
                if (!r.Config.Entries.TryGetValue(d.StringAcc, out var c))
                    c = r.Config;

                return new ConfigEntry(c, r.SequenceId);
            }).ToList()
        );
    }

    internal static void OperationLookupChangeDynamic(FplQuery parser, ParsingContext context, ParsingNode focus, ParseOperationData d)
    {

        var config = context.Focus.Configs[context.Focus.Active.ConfigId];
        var read = context.Focus.Reads[focus.ReadId];
        var intersections = context.Focus.GenerateSequencesIntersection(
            read, read.Select(x => x.SequenceId).ToList(),
            config, config.Select(x => x.SequenceId).ToList()
        );


        context.Focus.NextConfig(d.Id, intersections.Select(x =>
        {
            var acc = x.Primary.Value.V.ToString();
            var c = x.AVal.Foci[0].Config;

            if (c.Entries.TryGetValue(acc, out var c2))
                c = c2;

            return new ConfigEntry(c, x.Primary.SequenceId); ;
        }).ToList());
    }






}




