using FlexibleParsingLanguage.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Parse;

internal partial struct ParsesOperationType
{

    internal class ParserOperationCompileData { }

    internal static (List<ParseOperation>, OpCompileType) CompileOperations(ParseData data, ParseContext root)
    {
        var opsMap = data.OpsMap.ToDictionary(x => x.Value, x => x.Key);

        var saved = new Dictionary<int, ParseOperation>();

        var loaded = new HashSet<int>();

        var outOps = new List<ParseOperation>(data.Ops.Count);
        for (var i = data.Ops.Count - 1; i >= 0; i--)
        {
            var (id, op) = data.Ops[i];

            var meta = op.OpType.GetMetaData();

            if (op.OpType.Op == ParsesOperationType.Load)
                loaded.Add(op.IntAcc);
        }
        outOps.Reverse();

        foreach (var o in data.Ops.Select(x => x.Item2))
        {
            if (o.OpType.Op == ParsesOperationType.Load)
                loaded.Add(o.IntAcc);
        }


        foreach (var o in data.Ops.Select(x => x.Item2))
        {
            if (o.OpType.Op == ParsesOperationType.Load)
                loaded.Add(o.IntAcc);
        }

        var rootOpCompileType = OpCompileType.None;

        /*
        foreach (var (id, o) in data.Ops)
        {
            if (rootOpCompileType != OpCompileType.None)
                rootOpCompileType = o.OpType.GetOpCompileType();


            if (o.OpType.Op == ParsesOperationType.Save && !loaded.Contains(o.IntAcc))
                continue;

            if (o.OpType.Op == ParsesOperationType.WriteFlatten)
                o.IntAcc = (int)GetOpCompileType(opsParents[id]);

            outOps.Add(o);
        }
        */

        if (rootOpCompileType == OpCompileType.None)
            rootOpCompileType = OpCompileType.WriteArray;


        return (outOps, rootOpCompileType);
    }


    private static OpCompileType GetOpCompileType(HashSet<ParsesOperationType> opsTypes)
    {
        var ii = OpCompileType.None;
        foreach (var childOp in opsTypes)
        {
            var wt = childOp.GetOpCompileType();
            if (wt != OpCompileType.None)
                ii = wt;
        }


#if DEBUG
        var debug = opsTypes.Select(x => $"{x.GetMetaData().Name} {x.GetOpCompileType()}").Join("\n");
        var s = 345534;
#endif

        if (ii == OpCompileType.None)
            return OpCompileType.WriteArray;

        return ii;
    }




    internal static void Generic_Compile(ParserOperationCompileData opData)
    {

    }

}
