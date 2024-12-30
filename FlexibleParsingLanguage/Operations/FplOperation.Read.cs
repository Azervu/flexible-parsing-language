using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Operations;

internal static partial class FplOperation {

    internal static readonly OpConfig Read = new OpConfig(".", OpSequenceType.RightInput | OpSequenceType.LeftInput | OpSequenceType.Default, CompileRead);

   
    private static IEnumerable<ParseOperation> CompileRead(ParseData parser, RawOp op)
    {
        if (op.Input.Count != 2)
            throw new QueryException(op, $"{op.Input.Count} params | read takes 2");

        foreach (var x in FplOperation.EnsureLoaded(parser, op))
            yield return x;

        var input = op.Input[0];
        var accessor = op.Input[1];

        yield return new ParseOperation(OperationRead, accessor.Accessor);

        parser.ActiveId = op.Id;
        parser.LoadedId = op.Id;

        foreach (var x in FplOperation.EnsureSaved(parser, op))
            yield return x;
    }


    internal static void OperationRead(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.ReadFunc((m, readSrc) => m.Parse(readSrc, acc));
}















