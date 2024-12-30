﻿using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Operations;

internal partial class FplOperation
{
    internal static readonly OpConfig Foreach = new OpConfig("*", OpSequenceType.LeftInput, CompileForeach);

    private static IEnumerable<ParseOperation> CompileForeach(ParseData parser, RawOp op) {
        foreach (var x in CompileTransformOperation(parser, op, OperationForeach))
            yield return x;
    }

    internal static void OperationForeach(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.ReadFlatten();
}
