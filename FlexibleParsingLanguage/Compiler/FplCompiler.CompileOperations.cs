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
        var writeType = op.Type.CompileType & (OpCompileType.WriteArray | OpCompileType.WriteObject | OpCompileType.WriteFlexible);

        if (writeType == OpCompileType.None)
            return;

        if (writeType.Any(OpCompileType.WriteFlexible))
            writeType = op.Input.Count < 2 ? OpCompileType.WriteArray : OpCompileType.WriteObject;

        var v = (op.Id, writeType);
        ctx[op.Id] = (op.Id, writeType);

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

    internal FplQuery CompileOperations(List<RawOp> ops, ParsingMetaContext configContext)
    {
        var compiles = new List<ParseOperation>();
        var ranks = new HashSet<int>();


        var writeOutput = new Dictionary<int, (int, OpCompileType)>();
        foreach (var op in ops)
            HandleInput(writeOutput, (x) => x.Input, op);

        var parseData = new ParseData
        {
            Filters = _filters,
            Converter = _converter,
            LoadedId = [ReadRootId],
            WriteOutput = writeOutput,
            ActiveReadId = ReadRootId,
            ActiveWriteId = WriteRootId,
        };

        var compilesOps = new List<(int Id, List<ParseOperation> Ops)>();
        var x2 = ops.Select(x => x.Type.SequenceType.ToString()).Join(", ");

        RawOp? active = null;
        try
        {
            foreach (var op in ops.Where(x => x.Type.Compile != null))
            {
                active = op;
                CompileOp(op, parseData, compilesOps, writeOutput);
            }
        }
        catch (QueryException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new QueryException(active, $"Error compiling query | {ex.Message}", true, ex);
        }



        var outOps = compilesOps.SelectMany(x => x.Ops).ToList();
        var last = outOps[outOps.Count - 1];
        var rootTargetingOps = outOps.Where(x => x.Metadata.Input.Where(x => (x.Type.SequenceType & OpSequenceType.Root) > 0).Any()).ToList();
        return new FplQuery(outOps, configContext, _modules);
    }

    private void CompileOp(RawOp op, ParseData parseData, List<(int Id, List<ParseOperation> Ops)> compilesOps, Dictionary<int, (int, OpCompileType)> writeOutput)
    {
        var id = op.GetStatusId(parseData);
        var x = new List<ParseOperation>();

        if (op.ReadId == -1)
        {
            //TODO refactor out
            op.ReadId = parseData.ActiveReadId;
            op.WriteId = parseData.ActiveWriteId;
        }

        foreach (var o in op.Type.Compile(parseData, op))
        {
            o.Metadata = op;
            x.Add(o);
        }

        compilesOps.Add((id, x));
    }

}
