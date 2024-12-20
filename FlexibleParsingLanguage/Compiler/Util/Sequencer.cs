using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler.Util;

internal static class Sequencer
{

    private class SequenceWrapper
    {
        internal int LeftIndex { get; set; }
        internal int RightIndex { get; set; }
        internal int Index { get; set; }

        internal SequenceWrapper? Parent { get; set; }
        internal List<SequenceWrapper> Children { get; private set; } = new List<SequenceWrapper>();
        internal List<SequenceWrapper> Input { get; set; } = new List<SequenceWrapper>();
        internal List<SequenceWrapper> Output { get; set; } = new List<SequenceWrapper>();
        internal RawOp Op { get; set; }



        internal static void Sequence(List<RawOp> ops, OpConfig defaultOp)
        {

            var sequenceOps = new List<SequenceWrapper>(ops.Count);
            for (var i = 0; i < ops.Count; i++)
            {
                var op = ops[i];
                var seq = new SequenceWrapper
                {
                    Op = op,
                };
            }

            SequenceGroup(ref sequenceOps);
            SequenceAffixes(ref sequenceOps, defaultOp);
        }


        internal void AddLeftInput(int targetIndex, List<SequenceWrapper> opsTemp)
        {
            if (targetIndex >= 0)
            {
                AddInput(targetIndex, opsTemp);
                return;
            }
            if (targetIndex != -2)
                return;

            if (Parent == null)
            {
                Input.Add(null);
                return;
            }
        }

        internal void AddRightInput(int targetIndex, List<SequenceWrapper> opsTemp)
        {
            if (targetIndex >= 0)
            {
                AddInput(targetIndex, opsTemp);
                return;
            }
            if (targetIndex != -2)
                return;

            if (Parent == null)
            {
                Input.Add(null);
                return;
            }
        }

        internal void AddInput(int targetIndex, List<SequenceWrapper> opsTemp)
        {
            var seq = opsTemp[targetIndex];
            if (seq.LeftIndex >= 0 && seq.LeftIndex != Index)
                opsTemp[seq.LeftIndex].RightIndex = Index;

            if (seq.RightIndex >= 0 && seq.RightIndex != Index)
                opsTemp[seq.RightIndex].LeftIndex = Index;


            if (!Op.Type.Category.Has(OpCategory.Branch))
            {
                seq.LeftIndex = -1;
                seq.RightIndex = -1;
                if (Input.Count > 0 && Input[Input.Count - 1] != null)
                    seq.LeftIndex = Input[Input.Count - 1].Index;
            }

            Input.Add(seq);
        }
    };

    private static void SequenceGroup(ref List<SequenceWrapper> ops)
    {
        var stack = new List<SequenceWrapper> { };
        foreach (var op in ops)
        {
            if (stack.Count > 0)
            {
                var groupOp = stack[stack.Count - 1];
                if (stack.Count > 1 && op.Op.Type.Operator == groupOp.Op.Type.GroupOperator.ToString())
                {
                    stack.RemoveAt(stack.Count - 1);
                    continue;
                }
                op.Parent = groupOp;
                groupOp.Children.Add(op);
            }
            if (op.Op.Type.Category.Has(OpCategory.Group))
                stack.Add(op);
        }

        ops = ops.Where(x => !x.Op.Type.Category.Has(OpCategory.UnBranch)).ToList();
    }


    private  class SequenceStackEntry
    {
        internal int LeftIndex { get; set; } = -2;
        internal List<SequenceWrapper> AwaitingRightIndex = new();
    }
    private static void SequenceAffixes(ref List<SequenceWrapper> ops, OpConfig unknownOp)
    {

        var nullWrapper = new SequenceWrapper() { Op = new RawOp { Type = unknownOp } };
        var sequenceData = new Dictionary<SequenceWrapper, SequenceStackEntry>();

        for (var i = 0; i < ops.Count; i++)
        {
            var seq = ops[i];

            var key = seq.Parent ?? nullWrapper;
            if (!sequenceData.TryGetValue(key, out var entry))
            {
                entry = new SequenceStackEntry();
                sequenceData.Add(key, entry);
            }

            seq.Index = i;
            seq.LeftIndex = entry.LeftIndex;
            seq.RightIndex = -2;

            if (seq.Op.Type.Category.Has(OpCategory.Branch))
            {
                entry.AwaitingRightIndex.Add(seq);
            }
            else
            {
                foreach (var ar in entry.AwaitingRightIndex)
                    ar.RightIndex = i;
                entry.LeftIndex = i;
                entry.AwaitingRightIndex = [seq];
            }
        }


        var ordered = ops.Select(x => x).ToList();
        ordered.OrderByDescending(x => x.Op.Type.Rank).ThenBy(x => x.Index);
        foreach (var op in ordered)
        {
            if (op.Op.Type.Category.Has(OpCategory.Prefix))
                op.AddLeftInput(op.LeftIndex, ops);
            if (op.Op.Type.Category.Has(OpCategory.Postfix))
                op.AddRightInput(op.RightIndex, ops);
        }


        foreach (var op in ops)
        {
            foreach (var input in op.Input)
            {
                if (input == null)
                    continue;

                input.Output.Add(op);
            }
        }

        foreach (var op in ops)
        {
            if (!op.Op.Type.Category.Has(OpCategory.Group))
                continue;

            if (op.Children.Count == 0)
            {
                foreach (var o in op.Output)
                {
                    var i = o.Input.IndexOf(o);
                    o.Input[i] = new SequenceWrapper() { Op = new RawOp { Type = unknownOp } };
                }
                continue;
            }

            var first = op.Children.First();

            if (first.Op.Type.Category.Has())


        }

        var outOps = new List<SequenceWrapper>(ops.Count);
        foreach (var op in ops)
        {
            if (op.Op.Type.Category.Has(OpCategory.Group))
                continue;

            op.Op.Input = op.Input.Select(x => x?.Op).ToList();
            foreach (var input in op.Input)
            {
                if (input == null)
                    continue;
                input.Op.Output.Add(op.Op);
            }

            outOps.Add(op);
        }
        ops = outOps;


    }






}
