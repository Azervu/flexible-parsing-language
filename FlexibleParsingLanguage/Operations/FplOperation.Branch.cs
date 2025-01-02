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
    internal static readonly OpConfig Branch = new OpConfig("{", OpSequenceType.Root | OpSequenceType.Group | OpSequenceType.Branching | OpSequenceType.LeftInput, CompileBranch, 100, "}")
    {
        CompileType = OpCompileType.Branch,
        CompileRank = 100
    };

    private static IEnumerable<ParseOperation> CompileBranch(ParseData parser, RawOp op)
    {
        if (op.Input.Count != 1)
            throw new QueryException(op, "wrong number of params");

        foreach (var x in EnsureLoaded(parser, op))
            yield return x;

        var id = op.GetStatusId(parser);


        if (parser.ProccessedMetaData.TryGetValue(id, out var m) && (m.Type.CompileType & OpCompileType.WriteObject) > 0)
        {
            var accessor = m.Input[1].Accessor;
            yield return new ParseOperation(ParsesOperationType.WriteFromRead, accessor);
        }
        else
        {
            yield return new ParseOperation(ParsingContext.WriteAddRead);

        }
    }
}


    //internal static void OperationWrite(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.WriteStringFromRead(acc);