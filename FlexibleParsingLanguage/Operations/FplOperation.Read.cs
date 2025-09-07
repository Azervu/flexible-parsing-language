using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace FlexibleParsingLanguage.Operations;

internal static partial class FplOperation {

    internal static readonly OpConfig Read = new OpConfig(".", OpSequenceType.RightInput | OpSequenceType.LeftInput | OpSequenceType.Default)
    {
        Compile = CompileReadOperation,
        Sequence = SequenceRead,
    };

    private static void SequenceRead(SequenceProccessData d, RawOp o)
    {
        d.ActiveReadId = o.Id;
    }

    private static IEnumerable<ParseOperation> CompileReadOperation(ParseData parser, RawOp op)
    {
        if (op.Input.Count < 2)
            throw new QueryException(op, $"{op.Input.Count} params | read takes 2+");

        foreach (var x in CompileLoad(parser, op))
            yield return x;

        var inputOp = op.Input[0];
        var accessor = op.Input[1];
        var parameters = CompiledParameter.CompileParameters(parser, op, 1);
        var dynamic = parameters.Any(x => !x.IsLiteral);

        if (dynamic || parameters.Count >= 2)
        {
            var id = op.GetStatusId(parser);

            foreach (var x in FplOperation.CompileLoad(parser, op))
                yield return x;

            yield return new ParseOperation(op, (q, c, d) => OperationDynamicRead(q, c, d, parameters), id);
        }
        else if (accessor.Type.SequenceType.All(OpSequenceType.Literal) || !int.TryParse(accessor.Accessor, out var intAcc))
        {
            if (accessor.Accessor == null)
                throw new Exception("OperationRead null access");
            yield return new ParseOperation(op, OperationRead, new ParseOperationData { StringAcc = accessor.Accessor, ReadId = inputOp.ReadId, WriteId = inputOp.WriteId });
        }
        else
        {
            yield return new ParseOperation(op, OperationReadInt, new ParseOperationData { IntAcc = intAcc, ReadId = inputOp.ReadId, WriteId = inputOp.WriteId });
        }

        parser.LoadedId[0] = op.Id;

        parser.ActiveReadId = op.Id;
        op.ReadId = op.Id;


#if DEBUG
        if (op.Id == 12)
        {
            var s = 5434564;
        }
#endif
    }

    internal static void ReadFunc(this ParsingContext context, ParseOperationData d, Func<IReadingModule, object, object> readTransform)
    {
        var opId = d.Id;
        var reads = context.Focus.Reads;




        reads[opId] = reads[d.ReadId].Select((input) =>
        {


#if DEBUG

            var s = JsonSerializer.Serialize(reads);

#endif


            var r = input.Value;
            var result = readTransform(context.GetReadingModule(r), r.V);
            var output = new KeyValuePair<ValueWrapper, ValueWrapper>(r, new ValueWrapper(result));
            return new FocusEntry { Key = output.Key, Value = output.Value, SequenceId = input.SequenceId, };
        }).ToList();

        context.Focus.Store[opId] = new ParsingNode(opId, d.WriteId, context.Focus.Active.ConfigId);
        context.Focus.Active = context.Focus.Store[opId];

#if DEBUG
        if (opId == 12)
        {
            var s2 = 5434564;
        }
#endif
    }

    internal static void OperationRead(FplQuery parser, ParsingContext context, ParseOperationData d)
    {
        context.ReadFunc(d, (m, readSrc) => m.Parse(readSrc, d.StringAcc));

#if DEBUG
        if (d.Id == 12)
        {
            var s = 5434564;
        }
#endif
    }


    private static void OperationReadInt(FplQuery parser, ParsingContext context, ParseOperationData d)
    {
        context.ReadFunc(d, (m, readSrc) => m.Parse(readSrc, d.IntAcc));
    }

    private static void OperationDynamicRead(FplQuery q, ParsingContext c, ParseOperationData d, List<CompiledParameter> parameters)
    {
        var result = new List<FocusEntry>();

        var param = c.GetActiveParameters(parameters).ToList();
        foreach (var ps in param)
        {
            var m = c.GetReadingModule(ps.Primary);

            var r = new List<object>();
            object v = null;
            foreach (var p in ps.Secondary)
            {
                if (p.CompiledParameter.IsLiteral)
                {
                    if (int.TryParse(p.CompiledParameter.Accessor, out var i))
                        v = m.Parse(ps.Primary, i);
                    else
                        v = m.Parse(ps.Primary, p.CompiledParameter.Accessor);
                }
                else
                {
                    var intAcc = p.Value as int?;
                    if (intAcc != null)
                        v = m.Parse(ps.Primary, intAcc.Value);
                    else
                        v = m.Parse(ps.Primary, p.Value?.ToString());
                }
                r.Add(v);
            }
            result.Add(new FocusEntry
            {
                Key = new ValueWrapper(d.StringAcc),
                Value = new ValueWrapper(r.Count < 2 ? v : r),
                SequenceId = ps.SequenceId,
            });
        }
        c.Focus.NextRead(d.Id, result);

#if DEBUG



        if (d.Id == 12)
        {
            var s = 5434564;
        }
#endif
    }
}