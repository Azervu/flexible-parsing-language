using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
namespace FlexibleParsingLanguage.Operations;

internal static partial class FplOperation
{
    internal static readonly OpConfig Branch = new OpConfig("{", OpSequenceType.Root | OpSequenceType.Group | OpSequenceType.Branching | OpSequenceType.LeftInput, CompileBranch, 100, "}");

    private static IEnumerable<ParseOperation> CompileBranch(ParseData parser, RawOp op)
    {
        if (op.Input.Count != 1)
            throw new QueryCompileException(op, "wrong number of params");

        foreach (var x in EnsureLoaded(parser, op))
            yield return x;

        var input = op.Input[0];


        if (input.Type.CompileType == OpCompileType.None)
            yield return new ParseOperation(ParsesOperationType.WriteAddRead);
    }
}
