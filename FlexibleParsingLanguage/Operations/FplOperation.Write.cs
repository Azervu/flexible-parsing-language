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


        foreach (var x in EnsureLoaded(parser, op))
            yield return x;

        var input = op.Input[0];
        var accessor = op.Input[1];

        yield return new ParseOperation(WriteOperation, accessor.Accessor);

        parser.ActiveId = op.Id;
        parser.LoadedId = op.Id;

        foreach (var x in EnsureSaved(parser, op))
            yield return x;
    }

    internal static void WriteOperation(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.WriteStringFromRead(acc);

}
