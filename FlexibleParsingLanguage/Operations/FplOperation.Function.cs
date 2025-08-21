using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FlexibleParsingLanguage.Operations;

internal partial class FplOperation
{

    internal static readonly OpConfig Function = new OpConfig("|", OpSequenceType.LeftInput | OpSequenceType.RightInput | OpSequenceType.OptionalExtraInput, (p, o) => CompileFunction(p, o))
    {
        CompileType = OpCompileType.ReadArray,
    };

    private static IEnumerable<ParseOperation> CompileFunction(ParseData parser, RawOp op)
    {
        if (op.Input.Count < 2 || string.IsNullOrWhiteSpace(op.Input[1].Accessor))
            throw new QueryException(op, $"function withouth name");

        var acc = op.Input[1].Accessor;
        if (parser.Filters.TryGetValue(acc, out var f))
            return CompileFilterFunction(parser, op, f);

        if (parser.Converter.TryGetValue(acc, out var converter))
            return CompileTransformerFunction(parser, op, converter);

        throw new QueryException(op, $"unknown function '{acc}'");
    }

    private static IEnumerable<ParseOperation> CompileFilterFunction(ParseData parser, RawOp op, IFilterFunction func)
    {
        if (op.Input.Count < 2 || op.Input[1].Accessor == null)
            throw new QueryException(op, $"filter missing input");

        var parameters = CompiledParameter.CompileParameters(parser, op);

        return CompileSaveUtil(parser, op, -1, [new ParseOperation(op, (q, c, d) => OperationFunctionFilter(q, c, d, parameters, func))]);
    }

    internal static IEnumerable<ParseOperation> CompileTransformerFunction(ParseData parser, RawOp op, IConverterFunction converter)
    {
        var id = op.GetStatusId(parser);

        foreach (var x in FplOperation.EnsureLoaded(parser, op))
            yield return x;

        var parameters = CompiledParameter.CompileParameters(parser, op);

        yield return new ParseOperation(op, (q, c, d) => c.OperationFunctionConvertMultiParam(q, d, parameters, converter.Convert));

        parser.LoadedId[0] = id;

        foreach (var x in FplOperation.EnsureSaved(parser, op))
            yield return x;

        var sequences = new List<int>();
    }

    internal static void OperationFunctionFilter(FplQuery query, ParsingContext context, ParseOperationData d, List<CompiledParameter> parameters, IFilterFunction filter)
    {
        context.Focus.ReadMultiParamForeach(d.Id, parameters, (w, p) =>
        {
            var raw = context.GetReadingModule(w.Value).ExtractValue(w.Value.V);

            if (filter.Filter(raw, p))
                return [new KeyValuePair<object, object>(w.Key.V, w.Value.V)];

            return [];
        });
    }
}