using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Operations;

internal partial class FplOperation
{
    internal static readonly OpConfig Foreach = new OpConfig("*", OpSequenceType.RightInput | OpSequenceType.LeftInput, CompileForeach);

    private static IEnumerable<ParseOperation> CompileForeach(ParseData parser, RawOp op) => CompileTransformOperation(parser, op, OperationForeach);

    internal static void OperationForeach(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.ReadFlatten();
}
