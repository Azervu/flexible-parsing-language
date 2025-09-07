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

                ];
            }
            return _opConfigs;
        }
    }

    internal static readonly OpConfig Accessor = new OpConfig(null, OpSequenceType.Accessor, null);


    internal static readonly OpConfig Unescape = new OpConfig("\\", OpSequenceType.Unescape, null);

    internal static readonly OpConfig Branch = new OpConfig("{", OpSequenceType.Root | OpSequenceType.Group | OpSequenceType.Branching | OpSequenceType.LeftInput, null, "}");


    internal static IEnumerable<ParseOperation> CompileLoad(ParseData parser, RawOp op)
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

        parser.ActiveReadId = inputId;


        if (inputId < 0 || inputId == parser.LoadedId[0])
            yield break;

        parser.LoadedId[0] = inputId;

        if (inputId == Compiler.FplCompiler.ReadRootId)
            yield return new ParseOperation(op, ReadParamOperation);
        else
            yield return new ParseOperation(op, OperationLoad, inputId);
    }

    internal static void OperationLoad(FplQuery parser, ParsingContext context, ParseOperationData d)
    {

#if DEBUG
        if (!context.Focus.Store.ContainsKey(d.IntAcc))
        {
            var s = 5434564;
        }
#endif
        context.Focus.Active = context.Focus.Store[d.IntAcc];
    }

    internal static IEnumerable<ParseOperation> CompileSaved(ParseData parser, RawOp op)
    {
        var id = op.Type.GetStatusId != null
            ? op.Type.GetStatusId(parser, op)
            : op.Id;

        if (id == Compiler.FplCompiler.ReadRootId)
            yield break;

        yield return new ParseOperation(op, ParsesOperationType.Save, id);
    }

    internal static IEnumerable<ParseOperation> CompileSaveUtil(ParseData parser, RawOp op, int numInput, IEnumerable<ParseOperation> pos)
    {
        if (numInput >= 0 && op.Input.Count != numInput)
            throw new QueryException(op, $"{op.Input.Count} inputs, takes {numInput}");

        var id = op.GetStatusId(parser);

        foreach (var x in FplOperation.CompileLoad(parser, op))
            yield return x;

        foreach (var po in pos)
            yield return po;

        parser.LoadedId[0] = id;
        parser.ActiveReadId = id;
        op.ReadId = id;
    }

}
