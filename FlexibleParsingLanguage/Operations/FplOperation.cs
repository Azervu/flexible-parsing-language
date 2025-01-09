using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FlexibleParsingLanguage.Operations;


internal static partial class FplOperation
{
    private static List<OpConfig> _opConfigs;

    internal static List<OpConfig> OpConfigs { get
        {
            if (_opConfigs == null)
            {
                _opConfigs = [
                    Branch,
                    Read,
                    Foreach,
                    Write,
                    WriteForeach,
                    Param,
                    Lookup,
                    ChangeLookupContext,
                    Function,
                    new OpConfig(":$", OpSequenceType.LeftInput, (p, o) => CompileSaveUtil(p, o, 1, [new ParseOperation(ParsesOperationType.WriteRoot)])),
                    
                    new OpConfig("~", OpSequenceType.LeftInput, (p, o) => CompileSaveUtil(p, o, 1, [new ParseOperation(ParsesOperationType.ReadName)])),

                    new OpConfig("@", OpSequenceType.ParentInput | OpSequenceType.Virtual),
                    new OpConfig("\"", OpSequenceType.Literal | OpSequenceType.Accessor, null, -1, "\""),
                    new OpConfig("'", OpSequenceType.Literal | OpSequenceType.Accessor, null, -1, "\'"),
                    new OpConfig("\\", OpSequenceType.Unescape, null, -1),

                    new OpConfig(",", OpSequenceType.GroupSeparator),
                    new OpConfig("(", OpSequenceType.Group | OpSequenceType.Virtual | OpSequenceType.Accessor, null, 100, ")"),
                ];
            }
            return _opConfigs;
        }
    }

    internal static readonly OpConfig Accessor = new OpConfig(null, OpSequenceType.Accessor, null, 99);

    internal static IEnumerable<ParseOperation> CompileTransformOperation(ParseData parser, RawOp op, Action<FplQuery, ParsingContext, int, string> opAction)
    {
        if (op.Input.Count < 1)
            throw new QueryException(op, $"{op.Input.Count} params | read takes 2");

        foreach (var x in EnsureLoaded(parser, op))
            yield return x;

        yield return new ParseOperation(opAction);

        parser.ActiveId = op.Id;
        parser.LoadedId = op.Id;

        foreach (var x in EnsureSaved(parser, op))
            yield return x;
    }


    private static IEnumerable<ParseOperation> CompileAccessorOperation(ParseData parser, RawOp op, Action<FplQuery, ParsingContext, int, string> accessorAction, Action<FplQuery, ParsingContext, ParsingFocus> dynamicAccessorAction)
    {
        if (op.Input.Count != 2)
            throw new QueryException(op, $"{op.Input.Count} params | read takes 2");

        foreach (var x in EnsureLoaded(parser, op))
            yield return x;

        var input = op.Input[0];
        var accessor = op.Input[1];

        if (accessor.Accessor != null)
            yield return new ParseOperation(accessorAction, accessor.Accessor);
        else
            yield return new ParseOperation((q, c, i, a) => dynamicAccessorAction(q, c, c.Focus.Store[i]), accessor.Id);

        parser.ActiveId = op.Id;
        parser.LoadedId = op.Id;

        foreach (var x in EnsureSaved(parser, op))
            yield return x;
    }

    





    /*

    private static IEnumerable<ParseOperation> CompileAccessorOperationConfig(ParseData parser, RawOp op, Action<FplQuery, ParsingContext, int, string> accessorAction)
    {
        if (op.Input.Count != 2)
            throw new QueryException(op, $"{op.Input.Count} params | read takes 2");

        foreach (var x in EnsureLoaded(parser, op))
            yield return x;

        var input = op.Input[0];
        var accessor = op.Input[1];

        if (accessor.Accessor != null)
        {
            yield return new ParseOperation(accessorAction, accessor.Accessor);
        }
        else
        {
            yield return new ParseOperation((p, c, i, a) => OperationReadDynamicConfig(p, c, i, a, accessorAction), accessor.Id);
        }

        parser.ActiveId = op.Id;
        parser.LoadedId = op.Id;

        foreach (var x in EnsureSaved(parser, op))
            yield return x;
    }

    internal static void OperationReadDynamicConfig(FplQuery parser, ParsingContext context, int intAcc, string acc, Action<FplQuery, ParsingContext, int, string> accessorAction)
    {
        var config = context.Focus.Configs[context.Focus.Store[intAcc].ConfigId];


        var intersections = context.Focus.GenerateSequencesIntersection(context.Focus.Writes[context.Focus.Active.WriteId], [context.Focus.Reads[config]]);
        foreach (var x in intersections)
        {
            foreach (var a in x.Intersected)
            {
                foreach (var r in a.Foci)
                {
                    context.ReadFunc((m, readSrc) => m.Parse(readSrc, r.Value.V.ToString()));
                }
            }
        }
    }

    */































    /*

    internal static IEnumerable<ParseOperation> CompileStringAccessorOperation(ParseData parser, RawOp op, bool isRead, Action<FplQuery, ParsingContext, int, string> action)
    {
        if (op.Input.Count != 2)
            throw new QueryException(op, $"{op.Input.Count} params | read takes 2");

        foreach (var x in FplOperation.EnsureLoaded(parser, op))
            yield return x;

        var input = op.Input[0];
        var accessorInput = op.Input[1];


        if (accessorInput.Accessor != null)
        {
            yield return new ParseOperation((query, ctx, i, s) =>
            {

                //ctx.Focus
                var acc = ctx.Store[accessorInput.Id];



                //action(query, ctx, -1, acc);




                //ctx.ReadFunc((m, src) => transform(m, src, accessorInput.Accessor))



            });
        }
        else
        {
            yield return new ParseOperation(OperationRead, accessorInput.Accessor);
        }








        parser.ActiveId = op.Id;
        parser.LoadedId = op.Id;

        foreach (var x in FplOperation.EnsureSaved(parser, op))
            yield return x;

    }
    */

    /*
    var inputType = OpCompileType.ReadObject;

    if (parser.ReadInput.TryGetValue(op.Input[0].Id, out var v))
        inputType = v.Type;





    switch (inputType)
    {
        case OpCompileType.ReadObject:
            yield return new ParseOperation(OperationRead, accessor.Accessor);
            break;
        case OpCompileType.ReadArray:
            yield return new ParseOperation(OperationRead, accessor.Accessor);
            break;
    }
    */









    internal static IEnumerable<ParseOperation> CompileStringAccessorOperationOld(ParseData parser, RawOp op, bool isRead, Func<IReadingModule, object, string, object> transform)
    {
        if (op.Input.Count != 2)
            throw new QueryException(op, $"{op.Input.Count} params | read takes 2");

        foreach (var x in EnsureLoaded(parser, op))
            yield return x;

        var input = op.Input[0];
        var accessor = op.Input[1];

        if (accessor.Accessor != null)
        {
            yield return new ParseOperation((query, ctx, i, s) => ctx.ReadFunc((m, src) => transform(m, src, accessor.Accessor)));
        }
        else
        {
            var aId = accessor.GetStatusId(parser);
            yield return new ParseOperation((q, c, i, s) => OperationStringReadLoadAccessor(op, q, c, i, s, transform));
        }

        parser.ActiveId = op.Id;
        parser.LoadedId = op.Id;

        foreach (var x in EnsureSaved(parser, op))
            yield return x;
    }


    private static void OperationStringReadLoadAccessor(RawOp op, FplQuery query, ParsingContext ctx, int i, string s, Func<IReadingModule, object, string, object> transform)
    {


        throw new QueryException(op, $"Indirect accessor not supported yet");

        /*
        var stored = ctx.Store[i];

        var max = Math.Max(stored.Count, ctx.Focus.Count);

        if (
            (stored.Count != 1 && stored.Count != max)
            || (ctx.Focus.Count != 1 && ctx.Focus.Count != max)
            )
            throw new QueryException(op, $"num entries mismatch, must be equal or one of them must be | Focus = {ctx.Focus.Count} |  {stored.Count}");

        var results = new List<ParsingFocusEntry>();
       

        for (var index = 0; i < max; i++)
        {
            var a = stored[index];
            var b = ctx.Focus[index];
            var reads = b.Reads;
            a.Reads
        }
        ctx.Focus = results;
        */
    }

    internal static IEnumerable<ParseOperation> EnsureLoaded(ParseData parser, RawOp op)
    {
        var inputId = -1;

        if (op.Input.Count > 0)
        {
            var x = op.Input[0];
            inputId = x.Type.GetStatusId != null
                ? x.Type.GetStatusId(parser, x)
                : x.Id;
        }

        if (parser.LoadRedirect.TryGetValue(inputId, out var id2))
            inputId = id2;

        if (inputId < 0 || inputId == parser.LoadedId)
            yield break;

        parser.ActiveId = inputId;
        parser.LoadedId = inputId;

        if (inputId == Compiler.Compiler.RootId)
            yield return new ParseOperation(RootParamOperation);
        else
            yield return new ParseOperation(ParsesOperationType.Load, inputId);
    }


    internal static IEnumerable<ParseOperation> EnsureSaved(ParseData parser, RawOp op)
    {
        var id = op.Type.GetStatusId != null
            ? op.Type.GetStatusId(parser, op)
            : op.Id;

        if (id == Compiler.Compiler.RootId)
            yield break;

        yield return new ParseOperation(ParsesOperationType.Save, id);
    }





    /*
    internal static void HandleOp(ParseData parser, ParseOperation? op)
    {
        if (op == null)
            return;
        HandleOps(parser, [op]);
    }

    internal static void HandleOps(ParseData parser, ParseOperation[] ops)
    {
        var activeId = ops[0].OpType.Op == ParsesOperationType.ReadRoot ? -1 : parser.ActiveId;
        var key = (activeId, ops);
        if (parser.OpsMap.TryGetValue(key, out var readId))
        {
            parser.ActiveId = readId;
            return;
        }

        if (parser.ActiveId != parser.LoadedId)
        {
            if (!parser.SaveOps.Contains(parser.ActiveId))
                throw new Exception("Query parsing error | Unknown read id " + parser.ActiveId);
            parser.Ops.Add((-1, new ParseOperation(ParsesOperationType.Load, parser.ActiveId)));
            parser.LoadedId = parser.ActiveId;
        }


        parser.ActiveId = ++parser.IdCounter;
        parser.SaveOps.Add(parser.ActiveId);
        parser.LoadedId = parser.ActiveId;

        if (parser.OpsMap.ContainsKey(key))
            throw new Exception($"Repeated {key.activeId} ");

        foreach (var op in ops)
            parser.Ops.Add((parser.ActiveId, op));


        parser.OpsMap.Add(key, parser.ActiveId);
        parser.IdCounter++;
        var saveOp = new ParseOperation(ParsesOperationType.Save, parser.ActiveId);
        parser.Ops.Add((parser.IdCounter, saveOp));
        parser.OpsMap.Add((activeId, [saveOp]), parser.IdCounter);
    }
    */
    private static IEnumerable<ParseOperation> CompileSaveUtil(ParseData parser, RawOp op, int numInput, IEnumerable<ParseOperation> pos)
    {
        if (numInput >= 0 && op.Input.Count != numInput)
            throw new QueryException(op, $"{op.Input.Count} inputs, takes {numInput}");

        var id = op.GetStatusId(parser);

        foreach (var x in FplOperation.EnsureLoaded(parser, op))
            yield return x;

        foreach (var po in pos)
            yield return po;

        parser.ActiveId = id;
        parser.LoadedId = id;

        foreach (var x in FplOperation.EnsureSaved(parser, op))
            yield return x;
    }

}
