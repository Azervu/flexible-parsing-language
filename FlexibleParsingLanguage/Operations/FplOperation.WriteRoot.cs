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
    internal static readonly OpConfig WriteRoot = new OpConfig(":$", OpSequenceType.LeftInput, (p, o) => CompileSaveUtil(p, o, 1, WriteRootCompile))
    {

    };

    private static IEnumerable<ParseOperation> WriteRootCompile(ParseData parser, RawOp op, int id)
    {
        yield return new ParseOperation(ParsesOperationType.WriteRoot);
    }
}