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
    internal static readonly OpConfig Root = new OpConfig("$", OpSequenceType.RootParam)
    {
        Compile = CompileRootParam,
        GetStatusId = (data, op) => Compiler.FplCompiler.ReadRootId,
    };

    internal static readonly OpConfig WriteRoot = new OpConfig(":$", OpSequenceType.LeftInput)
    {
        Compile = CompileWriteRootParam,
        GetStatusId = (data, op) => Compiler.FplCompiler.ReadRootId,
    };

    private static IEnumerable<ParseOperation> CompileRootParam(ParseData parser, RawOp op)
    {
        if (op.Input.Count != 0)
            throw new QueryException(op, "$ can't take params");


        parser.LoadedId[0] = FplCompiler.ReadRootId;
        parser.ActiveReadId = FplCompiler.ReadRootId;
        op.ReadId = FplCompiler.ReadRootId;
        op.WriteId = parser.ActiveWriteId;

        /*
        if (parser.LoadedId[0] == FplCompiler.RootId)
            yield break;
        */


        //context.Focus.LoadRead(FplCompiler.RootId);


        yield return new ParseOperation(op, ReadParamOperation);
    }


    private static IEnumerable<ParseOperation> CompileWriteRootParam(ParseData parser, RawOp op)
    {
       // if (op.Input.Count != 0)
       //     throw new QueryException(op, "$: can't take params");

        if (parser.LoadedId[0] == FplCompiler.ReadRootId)
            yield break;

        parser.LoadedId[0] = FplCompiler.ReadRootId;
        parser.ActiveWriteId = FplCompiler.ReadRootId;
        op.ReadId = parser.ActiveReadId;
        op.WriteId = FplCompiler.ReadRootId;
        yield return new ParseOperation(op, WriteRootOperation);
    }

    internal static void ReadParamOperation(FplQuery parser, ParsingContext context, ParseOperationData d) {
        var readId = context.Focus.Store[FplCompiler.ReadRootId].ReadId;
        var a = context.Focus.Active;
        context.Focus.Active = new ParsingNode(readId, a.WriteId, a.ConfigId);
    }

    internal static void WriteRootOperation(FplQuery parser, ParsingContext context, ParseOperationData d)
    {
        var id = context.Focus.Store[FplCompiler.ReadRootId].WriteId;
        var a = context.Focus.Active;
        context.Focus.Active = new ParsingNode(a.ReadId, id, a.ConfigId);
    }
}