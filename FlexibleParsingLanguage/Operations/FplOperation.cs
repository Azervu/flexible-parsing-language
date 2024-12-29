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

    internal static readonly OpConfig RootParam = new OpConfig("$", OpSequenceType.RootParam, CompileRootParam);


    private static IEnumerable<ParseOperation> CompileRootParam(ParseData parser, RawOp op)
    {
        if (op.Input.Count != 0)
            throw new QueryCompileException(op, "$ cant take params");

        yield return new ParseOperation(RootParamOperation);


        /*
        var x =


        return new ParseOperation(
            (a, b) => { }

            );
        */
    }

    internal static void RootParamOperation(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.ToRootRead();



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
