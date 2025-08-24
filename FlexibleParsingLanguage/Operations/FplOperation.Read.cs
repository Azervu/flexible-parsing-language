using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace FlexibleParsingLanguage.Operations;

internal static partial class FplOperation {

    internal static readonly OpConfig Read = new OpConfig(".", OpSequenceType.RightInput | OpSequenceType.LeftInput | OpSequenceType.Default, CompileReadOperation);

    private static IEnumerable<ParseOperation> CompileReadOperation(ParseData parser, RawOp op)
    {
        if (op.Input.Count < 2)
            throw new QueryException(op, $"{op.Input.Count} params | read takes 2+");

        foreach (var x in EnsureLoaded(parser, op))
            yield return x;

        var input = op.Input[0];

        var accessor = op.Input[1];



        var parameters = CompiledParameter.CompileParameters(parser, op, 1);
        var dynamic = parameters.Any(x => !x.IsLiteral);


        if (dynamic || parameters.Count > 2)
        {
            var id = op.GetStatusId(parser);

            foreach (var x in FplOperation.EnsureLoaded(parser, op))
                yield return x;

            yield return new ParseOperation(op, (q, c, d) => OperationDynamicRead(q, c, d, parameters), id);
        }
        /*
        else if (parameters.Count > 2)
        {
            yield return new ParseOperation(op, (q, c, d) => OperationMultiRead(q, c, d, parameters), accessor.Id);
        }
        */
        else if (accessor.Type.SequenceType.All(OpSequenceType.Literal))
        {
            yield return new ParseOperation(op, OperationRead, accessor.Accessor);
        }
        else if (int.TryParse(accessor.Accessor, out var intAcc))
        {
            yield return new ParseOperation(op, OperationReadInt, intAcc);
        }
        else
        {
            yield return new ParseOperation(op, OperationRead, accessor.Accessor);
        }

        parser.LoadedId[0] = op.Id;
        foreach (var x in EnsureSaved(parser, op))
            yield return x;
    }

    internal static void ReadFunc(this ParsingContext context, int opId, Func<IReadingModule, object, object> readTransform) => context.Focus.Read(opId, (r) => {
#if DEBUG
        if (r.V == null)
            throw new Exception("Result is null");
#endif
        var result = readTransform(context.GetReadingModule(r), r.V);
        return new KeyValuePair<ValueWrapper, ValueWrapper>(r, new ValueWrapper(result));
    });


    internal static void OperationRead(FplQuery parser, ParsingContext context, ParseOperationData d)
    {
#if DEBUG
        if (d.StringAcc == null)
            throw new Exception("OperationRead null access");
#endif
        context.ReadFunc(d.Id, (m, readSrc) => m.Parse(readSrc, d.StringAcc));
    }


    private static void OperationReadInt(FplQuery parser, ParsingContext context, ParseOperationData d)
    {
        context.ReadFunc(d.Id, (m, readSrc) => m.Parse(readSrc, d.IntAcc));
    }

    private static void OperationDynamicRead(FplQuery q, ParsingContext c, ParseOperationData d, List<CompiledParameter> parameters)
    {
        var result = new List<FocusEntry>();

        var param = c.GetActiveParameters(parameters).ToList();
        foreach (var ps in param)
        {
            var m = c.GetReadingModule(ps.Primary);
            foreach (var p in ps.Secondary)
            {
                object x;
                if (p.CompiledParameter.IsLiteral)
                {
                    if (int.TryParse(p.CompiledParameter.Accessor, out var i))
                        x = m.Parse(ps.Primary, i);
                    else
                        x = m.Parse(ps.Primary, p.CompiledParameter.Accessor);
                }
                else
                {
                    var intAcc = p.Value as int?;
                    if (intAcc != null)
                        x = m.Parse(ps.Primary, intAcc.Value);
                    else
                        x = m.Parse(ps.Primary, p.Value?.ToString());
                }
                result.Add(new FocusEntry
                {
                    Key = new ValueWrapper(d.StringAcc),
                    Value = new ValueWrapper(x),    
                    SequenceId = ps.SequenceId,
                });
            }
        }
        c.Focus.NextRead(d.Id, result);
    }

    private static void OperationMultiRead(FplQuery parser, ParsingContext context, ParseOperationData d, List<CompiledParameter> parameters)
    {
        var result = new List<FocusEntry>();

        //TODO implement multi read

        /*
        foreach (var p in parameters)
        {
            object x;
            if (int.TryParse(p.CompiledParameter.Accessor, out var i))
                x = m.Parse(ps.Primary, i);
            else
                x = m.Parse(ps.Primary, p.CompiledParameter.Accessor);
        }
        */

    }


}