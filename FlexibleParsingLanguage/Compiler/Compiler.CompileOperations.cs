using FlexibleParsingLanguage.Operations;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler;

internal partial class Compiler
{
    internal FplQuery CompileOperations(List<RawOp> ops, ParsingMetaContext configContext, string query)
    {


        OpCompileType rootType = OpCompileType.None;


        var compiles = new List<ParseOperation>();

        var ranks = new HashSet<int>();





        /*
        foreach (var op in ops)
            ranks.Add(op.Id);

        foreach (var r in ranks.OrderDescending())
        {
            var removes = new HashSet<int>();

            foreach (var op in ops)
            {

            }
        }
        */

        var parseData = new ParseData
        {
            ActiveId = RootId,
            LoadedId = RootId,
            IdCounter = RootId + 1,
            Ops = [],
            SaveOps = [],
            OpsMap = new Dictionary<(int LastOp, ParseOperation[]), int>
            {
            },
        };

        var compilesOps = new List<(int Id, List<ParseOperation> Ops)>();

        foreach (var op in ops)
        {
            if (op.Type.Compile == null)
                continue;

            if (rootType == OpCompileType.None)
                rootType = op.Type.CompileType;

            var id = op.GetStatusId(parseData);
            var x = new List<ParseOperation>();

            foreach (var o in op.Type.Compile(parseData, op))
            {
                o.Metadata = op;
                x.Add(o);
            }



            compilesOps.Add((id, x));
        }

        if (rootType == OpCompileType.None)
            rootType = OpCompileType.WriteArray;


        var outOps = compilesOps.SelectMany(x => x.Ops).ToList();






        return new FplQuery(outOps, configContext, new ParserRootConfig { RootType = rootType }, query);
    }

}
