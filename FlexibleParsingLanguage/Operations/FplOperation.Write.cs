using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Operations;
internal static partial class FplOperation
{
    internal static readonly OpConfig Write = new OpConfig(":", OpSequenceType.RightInput | OpSequenceType.LeftInput, WriteCompile)
    {
        CompileType = OpCompileType.WriteObject,
    };

    internal static readonly OpConfig WriteForeach = new OpConfig(":*", OpSequenceType.LeftInput, WriteArrayCompile)
    {
        CompileType = OpCompileType.WriteArray,
    };

    private static IEnumerable<ParseOperation> WriteCompile(ParseData parser, RawOp op)
    {
        if (op.Input.Count != 2)
            throw new QueryException(op, $"{op.Input.Count} params | read takes 2");

        var id = op.GetStatusId(parser);

        var (writeId, writeType) = parser.WriteOutput[op.Output[0].Id];


        if (writeType == OpCompileType.Branch)
        {
            parser.ProccessedMetaData[writeId] = op;

            parser.LoadRedirect[op.GetStatusId(parser)] = op.Input[0].GetStatusId(parser);
            yield break;
        }


        foreach (var x in FplOperation.EnsureLoaded(parser, op))
            yield return x;

        var accessor = op.Input[1];

        switch (writeType)
        {
            case OpCompileType.WriteObject:
                yield return new ParseOperation(ParsesOperationType.Write, accessor.Accessor);
                break;
            case OpCompileType.WriteArray:

                yield return new ParseOperation(ParsesOperationType.WriteArray, accessor.Accessor);
                break;
        }

        parser.ActiveId = op.Id;
        parser.LoadedId = op.Id;

        foreach (var x in FplOperation.EnsureSaved(parser, op))
            yield return x;
    }


    private static IEnumerable<ParseOperation> WriteArrayCompile(ParseData parser, RawOp op)
    {
        if (op.Input.Count != 1)
            throw new QueryException(op, $"{op.Input.Count} write array | read takes 1");


        var input = op.Input[0];

        var id = op.GetStatusId(parser);


        var (writeId, writeType) = parser.WriteOutput[op.Output[0].Id];

        if (writeType == OpCompileType.Branch)
        {
            parser.ProccessedMetaData[writeId] = op;
            yield break;
        }

        foreach (var x in FplOperation.EnsureLoaded(parser, op))
            yield return x;

        switch (writeType)
        {
            case OpCompileType.WriteObject:
                yield return new ParseOperation(ParsesOperationType.WriteFlatten, 1);
                break;
            case OpCompileType.WriteArray:

                yield return new ParseOperation(ParsesOperationType.WriteFlatten, 2);
                break;
        }

        parser.ActiveId = op.Id;
        parser.LoadedId = op.Id;

        foreach (var x in FplOperation.EnsureSaved(parser, op))
            yield return x;
    }



    private static OpCompileType UtilWriteCheckDependencies(ParseData parser, RawOp op) {

        var branch = false;
        var array = false;
        var obj = false;



        var output = op.Output;

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

        var num = (obj ? 1 : 0) + (array ? 1 : 0) + (branch ? 1 : 0);
        if (num == 0)
            throw new QueryException(op, "has no write targets");

        if (num > 1)
            throw new QueryException(op, $"has multiple write targets = {(obj ? "obj, " : string.Empty)}{(array ? "array," : string.Empty)}{(branch ? "branch" : string.Empty)}");


        if (branch)
        {
            return OpCompileType.Branch;
        }
       

        if (array)
            return OpCompileType.WriteArray;

        return OpCompileType.WriteObject;
    }
}