using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

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
            return HandleFilter(parser, op, f);

        if (parser.Converter.TryGetValue(acc, out var converter))
            return CompileSaveUtil(parser, op, 2, [new ParseOperation((q, c, i, a) => OperationFunctionConvert(q, c, i, a, converter))]);

        throw new QueryException(op, $"unknown function '{acc}'");
    }


    private static IEnumerable<ParseOperation> HandleFilter(ParseData parser, RawOp op, IFilterFunction func)
    {
        if (op.Input.Count != 3 || op.Input[2].Accessor == null)
            throw new QueryException(op, $"filter missing input");

        var sequences = new List<string>();

        for (var i = 2; i < op.Input.Count; i++)
        {
            var o = op.Input[i];

            if (o.Accessor == null)
                throw new QueryException(op, $"dynamic parameter not yet supported");

            sequences.Add(o.Accessor);
        }

        return CompileSaveUtil(parser, op, -1, [new ParseOperation((q, c, i, a) => OperationFunctionFilter(q, c, i, a, func), op.Input[2].Accessor)]);
    }


    internal static void OperationFunctionFilter(FplQuery parser, ParsingContext context, int intAcc, string acc, IFilterFunction filter)
    {
        context.Focus.ReadForeach((w) =>
        {
            context.UpdateReadModule(w.Value);
            object raw;
            if (context.ReadingModule != null)
                raw = context.ReadingModule.ExtractValue(w.Value.V);
            else
                raw = w.Value.V;

            if (filter.Filter(raw, [acc]))
                return [new KeyValuePair<object, object>(w.Key.V, w.Value.V)];

            return [];
        });
        /*
        if (parser._filters.TryGetValue(acc, out var f))
        {
            context.Focus.ReadForeach((w) =>
            {
                context.UpdateReadModule(w);
                object raw;
                if (context.ReadingModule != null)
                    raw = context.ReadingModule.ExtractValue(w);
                else
                    raw = w.V;

                if (f.Filter())

                if (f.Convert(raw, out var result))
                    return new List<KeyValuePair<object, object>> {
                    new KeyValuePair<object, object>(w.V, result)
                };

                return new List<KeyValuePair<object, object>>();
            });
        }
        */

    }

    internal static void OperationFunctionConvert(FplQuery parser, ParsingContext context, int intAcc, string acc, IConverterFunction converter)
    {
        context.ReadTransformValue((w) =>
        {
            context.UpdateReadModule(new ValueWrapper(w));
            object raw;
            if (context.ReadingModule != null)
                raw = context.ReadingModule.ExtractValue(w);
            else
                raw = w;
            return converter.Convert(raw);
        });
    }
}