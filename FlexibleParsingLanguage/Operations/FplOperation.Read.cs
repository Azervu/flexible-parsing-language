using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Operations;

internal static partial class FplOperation {

    internal static readonly OpConfig Read = new OpConfig(".", OpSequenceType.RightInput | OpSequenceType.LeftInput | OpSequenceType.Default, CompileRead);

    private static IEnumerable<ParseOperation> CompileRead(ParseData parser, RawOp op)
    {
        if (op.Input.Count != 2)
            throw new QueryCompileException(op, $"{op.Input.Count} params | read takes 2");

        var input = op.Input[0];
        var accessor = op.Input[1];

        yield return new ParseOperation(ReadOperation, accessor.Accessor);
    }

    internal static void ReadOperation(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.ReadFunc((m, readSrc) => m.Parse(readSrc, acc));

}















