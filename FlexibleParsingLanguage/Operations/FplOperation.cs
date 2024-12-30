using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FlexibleParsingLanguage.Operations;

internal static partial class FplOperation
{

    internal static readonly OpConfig Accessor = new OpConfig(null, OpSequenceType.Accessor, null, 99);


    internal static IEnumerable<ParseOperation> EnsureLoaded(ParseData parser, RawOp op)
    {

        var inputId = -1;

        foreach (var inp in op.Input)
        {
            if (inp.Input.Count > 0)
                inputId = inp.Id;
        }

        if (inputId < 0 || inputId == parser.LoadedId)
            yield break;

        parser.LoadedId = inputId;
        yield return new ParseOperation(ParsesOperationType.Load, inputId);
    }


    internal static IEnumerable<ParseOperation> EnsureSaved(ParseData parser, RawOp op)
    {
        if (op.Output.Count <= 1)
            yield break;

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
}
