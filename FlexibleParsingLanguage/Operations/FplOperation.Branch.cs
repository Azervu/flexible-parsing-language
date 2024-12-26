using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Compiler.Util;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace FlexibleParsingLanguage.Operations;

internal static partial class FplOperation
{
    internal static readonly OpConfig Branch = new OpConfig("{", OpCategory.Root | OpCategory.Group | OpCategory.Branching | OpCategory.LeftInput, CompileBranch, 100, "}");

    //                    new OpConfig("{", OpCategory.Root | OpCategory.Group | OpCategory.Branching | OpCategory.LeftInput, null, 100, "}"),


    private static IEnumerable<ParseOperation> CompileBranch(ParseData parser, RawOp op)
    {
        if (op.Input.Count != 1)
            throw new QueryCompileException(op, "wrong number of params");

        var input = op.Input[0];



        yield return new ParseOperation(BranchOperation, input.Accessor);


        /*
        var x =


        return new ParseOperation(
            (a, b) => { }

            );
        */
    }

    internal static void BranchOperation(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.ReadFunc((m, readSrc) => m.Parse(readSrc, acc));

}
