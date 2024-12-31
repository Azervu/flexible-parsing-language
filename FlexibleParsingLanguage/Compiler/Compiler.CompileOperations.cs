using FlexibleParsingLanguage.Operations;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace FlexibleParsingLanguage.Compiler;

internal partial class Compiler
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

        var readInput = new Dictionary<int, (int, OpCompileType)>();
        var readOutput = new Dictionary<int, (int, OpCompileType)>();
        var writeInput = new Dictionary<int, (int, OpCompileType)>();
        var writeOutput = new Dictionary<int, (int, OpCompileType)>();
        foreach (var op in ops)
        {
            var ct = op.Type.CompileType;
            var wt = ct & (OpCompileType.WriteArray | OpCompileType.WriteObject | OpCompileType.Branch);
            var rt = ct & (OpCompileType.ReadArray | OpCompileType.ReadObject);

            if (wt != OpCompileType.None)
            {
                writeInput[op.Id] = (op.Id, wt);
                writeOutput[op.Id] = (op.Id, wt);
            }

            if (rt != OpCompileType.None)
            {
                readInput[op.Id] = (op.Id, rt);
                readOutput[op.Id] = (op.Id, rt);
            }
        }

        foreach (var op in ops)
        {
            HandleInput(readInput, (x) =>  x.Output, op);
            HandleInput(writeInput, (x) => x.Output, op);
            HandleInput(readOutput, (x) => x.Input, op);
            HandleInput(writeOutput, (x) => x.Input, op);
        }

        var parseData = new ParseData
        {
            ActiveId = RootId,
            LoadedId = RootId,
            Ops = [],
            OpsMap = new Dictionary<(int LastOp, ParseOperation[]), int> {},

            ReadInput = readInput,
            WriteInput = writeInput,
            ReadOutput = readOutput,
            WriteOutput = writeOutput,
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
