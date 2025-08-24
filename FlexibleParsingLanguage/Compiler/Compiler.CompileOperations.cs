using FlexibleParsingLanguage.Operations;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler;

public partial class FplCompiler
{
    private void HandleInput(Dictionary<int, (int, OpCompileType)> ctx, Func<RawOp, List<RawOp>> extractTest, RawOp op)
    {
        if (!ctx.TryGetValue(op.Id, out var v))
            return;

        List<RawOp> current = [op];

        while (current.Count > 0) {

            var next = new List<RawOp>();
            foreach (var o in current)
            {
                foreach (var t in extractTest(o))
                {
                    if (ctx.ContainsKey(t.Id))
                        continue;
                    ctx[t.Id] = v;
                    next.Add(t);
                }
            }
            current = next;
        }
    }

    internal FplQuery CompileOperations(List<RawOp> ops, ParsingMetaContext configContext, string query)
    {
        OpCompileType rootType = OpCompileType.None;
        var compiles = new List<ParseOperation>();
        var ranks = new HashSet<int>();


        var writeOutput = new Dictionary<int, (int, OpCompileType)>();
        foreach (var op in ops)
        {
            var ct = op.Type.CompileType;
            var wt = ct & (OpCompileType.WriteArray | OpCompileType.WriteObject | OpCompileType.Branch);
            var rt = ct & (OpCompileType.ReadArray | OpCompileType.ReadObject);

            if (wt != OpCompileType.None)
                writeOutput[op.Id] = (op.Id, wt);

        }

        foreach (var op in ops)
            HandleInput(writeOutput, (x) => x.Input, op);

        var parseData = new ParseData
        {
            Filters = _filters,
            Converter = _converter,
            LoadedId = [RootId],
            WriteOutput = writeOutput,
        };


        var compilesOps = new List<(int Id, List<ParseOperation> Ops)>();

        foreach (var op in ops)
        {
            if (op.Type.Compile == null)
                continue;

            if ((rootType & (OpCompileType.WriteArray | OpCompileType.WriteObject)) == 0)
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

        if ((rootType & OpCompileType.WriteObject) > 0)
            rootType = OpCompileType.WriteObject;
        else
            rootType = OpCompileType.WriteArray;

        var outOps = compilesOps.SelectMany(x => x.Ops).ToList();

        return new FplQuery(outOps, configContext, new ParserRootConfig { RootType = rootType }, _modules, query);
    }

}
