using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Operations;

internal static partial class FplOperation
{

    internal static readonly OpConfig Lookup = new OpConfig("#", OpSequenceType.RightInput | OpSequenceType.LeftInput, (p, op) => CompileAccessorOperation(p, op, OperationLookup));

    internal static readonly OpConfig ChangeLookupContext = new OpConfig("##", OpSequenceType.RightInput | OpSequenceType.LeftInput, CompileLookup);

    internal static void OperationLookup(FplQuery parser, ParsingContext context, int intAcc, string acc)
    {
#if DEBUG
        if (acc == null)
            throw new Exception("OperationRead null access");
#endif


        context.ReadFunc((m, readSrc) => m.Parse(readSrc, acc));
    }

    private static IEnumerable<ParseOperation> CompileLookup(ParseData parser, RawOp op)
    {
        if (op.Input.Count != 2)
            throw new QueryException(op, $"{op.Input.Count} params | read takes 2");

        foreach (var x in FplOperation.EnsureLoaded(parser, op))
            yield return x;

        var input = op.Input[0];
        var accessor = op.Input[1];

        var inputType = OpCompileType.ReadObject;

        if (parser.ReadInput.TryGetValue(op.Input[0].Id, out var v))
            inputType = v.Type;

        if (accessor.Accessor == null)
        {
            var sdf = 345354;
        }

        switch (inputType)
        {
            case OpCompileType.ReadObject:
                yield return new ParseOperation(OperationRead, accessor.Accessor);
                break;
            case OpCompileType.ReadArray:
                yield return new ParseOperation(OperationRead, accessor.Accessor);
                break;
        }

        parser.ActiveId = op.Id;
        parser.LoadedId = op.Id;

        foreach (var x in FplOperation.EnsureSaved(parser, op))
            yield return x;
    }


}




