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
    internal static readonly OpConfig Write = new OpConfig(":", OpSequenceType.RightInput | OpSequenceType.LeftInput, WriteCompile)
    {
        CompileType = OpCompileType.WriteObject,
    };

    private static IEnumerable<ParseOperation> WriteCompile(ParseData parser, RawOp op)
    {
        if (op.Input.Count != 2)
            throw new QueryException(op, $"{op.Input.Count} params | read takes 2");


        var input = op.Input[0];
        var accessor = op.Input[1];

        var id = op.GetStatusId(parser);


        var output = op.Output;

        var branch = false;
        var array = false;
        var obj = false;

        while (output.Count > 0)
        {
            var next = new List<RawOp>();
            foreach (var o in output)
            {
                if (o.Type.CompileType.All(OpCompileType.Branch))
                {
                    var oId = o.GetStatusId(parser);
                    if (parser.ProccessedMetaData.ContainsKey(oId))
                        throw new QueryException(op, "Multi inheritance branch");
                    parser.ProccessedMetaData[oId] = op;
                    branch = true;
                }
                else if (o.Type.CompileType.All(OpCompileType.WriteObject))
                {
                    obj = true;
                }
                else if (o.Type.CompileType.All(OpCompileType.WriteArray))
                {
                    array = true;
                }
                else
                {
                    foreach (var o2 in o.Output)
                        next.Add(o2);
                }
            }
            output = next;
        }


        if (!obj && !array)
        {
            if (!branch)
                throw new QueryException(op, "has no write targets");

            parser.LoadRedirect[op.GetStatusId(parser)] = input.GetStatusId(parser);
            yield break;
        }

        if (obj && array)
            throw new QueryException(op, "has both object and array write children");

        if (branch)
            throw new QueryException(op, "has both branch and write");

        foreach (var x in FplOperation.EnsureLoaded(parser, op))
            yield return x;

        yield return new ParseOperation(obj ? ParsesOperationType.Write : ParsesOperationType.WriteArray, accessor.Accessor);

        parser.ActiveId = op.Id;
        parser.LoadedId = op.Id;

        foreach (var x in FplOperation.EnsureSaved(parser, op))
            yield return x;
    }
}
