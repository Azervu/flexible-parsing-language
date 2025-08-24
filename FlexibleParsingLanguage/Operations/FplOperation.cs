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
                    new OpConfig("[", OpSequenceType.LeftInput | OpSequenceType.Group | OpSequenceType.Virtual | OpSequenceType.Accessor, null, "]"),
                    Foreach,
                    Write,
                    WriteForeach,
                    Param,
                    WriteParam,
                    Lookup,
                    ChangeLookupContext,
                    Function,

                    SetVariable,
                    AccessVariable,

                    new OpConfig("~", OpSequenceType.LeftInput, (p, o) => CompileSaveUtil(p, o, 1, [new ParseOperation(o, ParsesOperationType.ReadName)])),
                    new OpConfig("\"", OpSequenceType.Literal | OpSequenceType.Accessor, null, "\""),
                    new OpConfig("'", OpSequenceType.Literal | OpSequenceType.Accessor, null, "\'"),
                    new OpConfig("\\", OpSequenceType.Unescape, null),

                    new OpConfig(",", OpSequenceType.GroupSeparator),
                    new OpConfig("(", OpSequenceType.Group | OpSequenceType.Virtual | OpSequenceType.Accessor, null, ")"),
                ];
            }
            return _opConfigs;
        }
    }

    internal static readonly OpConfig Accessor = new OpConfig(null, OpSequenceType.Accessor, null);

    private static IEnumerable<ParseOperation> CompileAccessorOperation(
        ParseData parser,
        RawOp op,
        Action<FplQuery, ParsingContext, ParseOperationData> accessorAction,
        Action<FplQuery, ParsingContext, ParseOperationData>? intAccessorAction,
        Action<FplQuery, ParsingContext, ParsingNode, ParseOperationData> dynamicAccessorAction
    )
    {
        if (op.Input.Count < 2)
            throw new QueryException(op, $"{op.Input.Count} params | read takes 2+");

        foreach (var x in EnsureLoaded(parser, op))
            yield return x;


        if (op.Input.Count > 2)
            throw new QueryException(op, $"{op.Input.Count} params | read takes 2>");



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

        foreach (var x in EnsureSaved(parser, op))
            yield return x;
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

        if (inputId < 0 || inputId == parser.LoadedId[0])
            yield break;

        parser.LoadedId[0] = inputId;

        if (inputId == Compiler.FplCompiler.RootId)
            yield return new ParseOperation(op, ReadParamOperation);
        else
            yield return new ParseOperation(op, ParsesOperationType.Load, inputId);
    }

    internal static IEnumerable<ParseOperation> EnsureSaved(ParseData parser, RawOp op)
    {
        var id = op.Type.GetStatusId != null
            ? op.Type.GetStatusId(parser, op)
            : op.Id;

        if (id == Compiler.FplCompiler.RootId)
            yield break;

        yield return new ParseOperation(op, ParsesOperationType.Save, id);
    }

    private static IEnumerable<ParseOperation> CompileSaveUtil(ParseData parser, RawOp op, int numInput, IEnumerable<ParseOperation> pos)
    {
        if (numInput >= 0 && op.Input.Count != numInput)
            throw new QueryException(op, $"{op.Input.Count} inputs, takes {numInput}");

        var id = op.GetStatusId(parser);

        foreach (var x in FplOperation.EnsureLoaded(parser, op))
            yield return x;

        foreach (var po in pos)
            yield return po;

        parser.LoadedId[0] = id;

        foreach (var x in FplOperation.EnsureSaved(parser, op))
            yield return x;
    }

}
