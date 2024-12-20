using FlexibleParsingLanguage.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Parse;

internal partial struct ParsesOperationType
{

    internal class ParserOperationCompileData
    {

    }


    internal static (List<ParseOperation>, WriteType) CompileOperations(ParseData data, ParseContext root)
    {
        var opsMap = data.OpsMap.ToDictionary(x => x.Value, x => x.Key);

        var saved = new Dictionary<int, ParseOperation>();


        var loaded = new HashSet<int>();



        var outOps = new List<ParseOperation>(data.Ops.Count);
        for (var i = data.Ops.Count - 1; i >= 0; i--)
        {
            var (id, op) = data.Ops[i];

            var meta = op.OpType.GetMetaData();




            switch (op.OpType.Op)
            {

            }

            if (op.OpType.Op == ParsesOperationType.Load)
                loaded.Add(op.IntAcc);

        }
        outOps.Reverse();


        foreach (var o in data.Ops.Select(x => x.Item2))
        {
            if (o.OpType.Op == ParsesOperationType.Load)
                loaded.Add(o.IntAcc);
        }







        var opsParents = data.OpsMap.GroupBy(x => x.Key.Item1).ToDictionary(
            x => x.Key,
            x => x.Select(y => y.Key.Item2.Last().OpType).ToHashSet()
        );

        var opsChildren = data.OpsMap.GroupBy(x => x.Value).ToDictionary(
            x => x.Key,
            x => x.Select(y => y.Key.Item2.Last().OpType).ToHashSet()
        );



        foreach (var o in data.Ops.Select(x => x.Item2))
        {
            if (o.OpType.Op == ParsesOperationType.Load)
                loaded.Add(o.IntAcc);
        }


        var rootWriteType = WriteType.None;


        foreach (var (id, o) in data.Ops)
        {
            if (rootWriteType != WriteType.None)
                rootWriteType = o.OpType.GetWriteType();


            if (o.OpType.Op == ParsesOperationType.Save && !loaded.Contains(o.IntAcc))
                continue;

            if (o.OpType.Op == ParsesOperationType.WriteFlatten)
                o.IntAcc = (int)GetWriteType(opsParents[id]);

            outOps.Add(o);
        }

        if (rootWriteType == WriteType.None)
            rootWriteType = WriteType.Array;


        return (outOps, rootWriteType);
    }


    private static WriteType GetWriteType(HashSet<ParsesOperationType> opsTypes)
    {
        var ii = WriteType.None;
        foreach (var childOp in opsTypes)
        {
            var wt = childOp.GetWriteType();
            if (wt != WriteType.None)
                ii = wt;
        }


#if DEBUG
        var debug = opsTypes.Select(x => $"{x.GetMetaData().Name} {x.GetWriteType()}").Join("\n");
        var s = 345534;
#endif

        if (ii == WriteType.None)
            return WriteType.Array;

        return ii;
    }




    internal static void Generic_Compile(ParserOperationCompileData opData)
    {

    }

}
