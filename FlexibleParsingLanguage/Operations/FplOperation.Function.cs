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

        foreach (var x in CompileSaveUtil(parser, op, -1, [new ParseOperation(op, (q, c, d) => OperationFilterFunction(q, c, d, parameters, func))]))
            yield return x;

        op.ReadId = op.Id;
        parser.ActiveReadId = op.Id;
    }

    internal static IEnumerable<ParseOperation> CompileTransformerFunction(ParseData parser, RawOp op, ITransformerFunction converter)
    {
        var id = op.GetStatusId(parser);

        foreach (var x in FplOperation.CompileLoad(parser, op))
            yield return x;

        var parameters = CompiledParameter.CompileParameters(parser, op);

        yield return new ParseOperation(op, (q, c, d) => c.OperationTransformerFunction(q, d, parameters, converter.Convert));

        parser.LoadedId[0] = id;
        op.ReadId = op.Id;
        parser.ActiveReadId = op.Id;
    }


    internal static void OperationTransformerFunction(this ParsingContext c, FplQuery parser, ParseOperationData data, List<CompiledParameter> parameters, Func<object, object[], object> converter)
    {
#if DEBUG
        c.Focus.ValidateTree();
#endif
        var result = new List<FocusEntry>();
        foreach (var p in c.GetActiveParameters(parameters))
        {
            var v = converter.Invoke(p.Primary, p.Secondary.Select(x => x.Value).ToArray());
            result.Add(new FocusEntry
            {
                Key = new ValueWrapper(data.StringAcc),
                Value = new ValueWrapper(v),
                SequenceId = p.SequenceId,
            });
        }
        c.Focus.NextRead(data.Id, result);

#if DEBUG
        c.Focus.ValidateTree();
#endif
    }

    internal static void OperationFilterFunction(FplQuery query, ParsingContext c, ParseOperationData d, List<CompiledParameter> parameters, IFilterFunction filter)
    {
        var result = new List<FocusEntry>();
        foreach (var p in c.GetActiveParameters(parameters))
        {
            if (filter.Filter(p.Primary, p.Secondary.Select(x => x.Value).ToArray()))
            {
                result.Add(new FocusEntry
                {
                    Key = new ValueWrapper(d.StringAcc),
                    Value = new ValueWrapper(p.Primary),
                    SequenceId = p.SequenceId,
                });
            }

        }
        c.Focus.NextRead(d.Id, result);
    }
}