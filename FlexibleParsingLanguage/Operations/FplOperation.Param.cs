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
    internal static readonly OpConfig Param = new OpConfig("$", OpSequenceType.RootParam)
    {
        Compile = CompileRootParam,
        GetStatusId = (data, op) => Compiler.FplCompiler.RootId,
    };

    internal static readonly OpConfig WriteParam = new OpConfig(":$", OpSequenceType.LeftInput, (p, o) => CompileSaveUtil(p, o, 1, [new ParseOperation(WriteRootOperation)]));

    private static IEnumerable<ParseOperation> CompileRootParam(ParseData parser, RawOp op)
    {
        if (op.Input.Count != 0)
            throw new QueryException(op, "$ can't take params");

        var id = op.Type.GetStatusId(parser, op);

        if (parser.LoadedId == id)
            yield break;

        parser.ActiveId = id;
        parser.LoadedId = id;

        yield return new ParseOperation(ReadParamOperation);
    }

    internal static void ReadParamOperation(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.Focus.LoadRead(Compiler.FplCompiler.RootId);

    internal static void WriteRootOperation(FplQuery parser, ParsingContext context, int intAcc, string acc) => context.Focus.LoadWrite(Compiler.FplCompiler.RootId);
}