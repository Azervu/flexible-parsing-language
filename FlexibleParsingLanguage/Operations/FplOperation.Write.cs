using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Operations;
internal static partial class FplOperation
{
    internal static readonly OpConfig Write = new OpConfig(":", OpSequenceType.RightInput | OpSequenceType.LeftInput, OpCompileType.WriteObject, WriteCompile);

    private static IEnumerable<ParseOperation> WriteCompile(ParseData parser, RawOp op)
    {
        if (op.Input.Count != 2)
            throw new QueryCompileException(op, $"{op.Input.Count} params | read takes 2");

        var input = op.Input[0];
        var accessor = op.Input[1];

        yield return new ParseOperation(WriteOperation, accessor.Accessor);
    }

    internal static void WriteOperation(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.WriteStringFromRead(acc);

}
