using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Operations;

internal partial class FplOperation
{
    internal static readonly OpConfig Foreach = new OpConfig("*", OpSequenceType.LeftInput, (p, o) => CompileSaveUtil(p, o, 1, [new ParseOperation(o, OperationForeach)]))
    {
        CompileType = OpCompileType.ReadArray,
    };

    internal static void OperationForeach(FplQuery parser, ParsingContext context, ParseOperationData op)
        => context.Focus.ReadForeach(op.Id, (r) => context.GetReadingModule(r.Value).Foreach(r.Value.V));

}