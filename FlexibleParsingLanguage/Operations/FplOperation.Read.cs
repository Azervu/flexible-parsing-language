using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Compiler.Util;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Operations;

internal static partial class FplOperation {

    internal static readonly OpConfig Read = new OpConfig(".", OpCategory.RightInput | OpCategory.LeftInput | OpCategory.Default, CompileRead);

    private static IEnumerable<ParseOperation> CompileRead(ParseData parser, RawOp op)
    {
        if (op.Input.Count != 1)
            throw new QueryCompileException(op, "wrong number of params");

        var input = op.Input[0];



        yield return new ParseOperation(ReadOperation, input.Accessor);


        /*
        var x =


        return new ParseOperation(
            (a, b) => { }

            );
        */
    }

    internal static void ReadOperation(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.ReadFunc((m, readSrc) => m.Parse(readSrc, acc));

}















