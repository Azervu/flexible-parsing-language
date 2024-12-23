using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FlexibleParsingLanguage.Compiler.Util;

internal partial class Lexicalizer
{

    internal void Sequence(ref List<RawOp> ops)
    {
        var data = new SequenceProccessData();
        var entries = new List<int>();

        foreach (var op in ops)
        {
            data.Ops.Add(op.Id, op);
            entries.Add(op.Id);
        }

        GroupOps(data, entries);


        var ordered = entries.Select(x => data.Ops[x]).ToList();
        ordered.OrderByDescending(x => x.Type.Rank).ThenBy(x => x.Id);
        foreach (var op in ordered)
        {
            if (op.Id == RootOpId)
                continue;
            data.SequenceAffixes(op);
        }

        foreach (var op in ops)
        {
            foreach (var op2 in op.GetInput())
            {
                op2.Output.Add(op);
            }
        }

        foreach (var op in ops)
            RemapInput(data, op);

        foreach (var op in ops)
            RemapBranchingGroupsInput(data, op);

        var proccessed = new HashSet<int>();

        var dependencyToWaiting = new Dictionary<int, List<int>>();
        var waitingOnDependencies = new Dictionary<int, (HashSet<int>, RawOp)>();

        var outOps = new List<RawOp>(ops.Count);
        foreach (var op in ops)
        {
            if ((op.Type.Category & (OpCategory.Virtual | OpCategory.UnGroup)) > 0)
                continue;

            if (op.Id == RootOpId)
            {
                op.LeftInput.Clear();
                op.RightInput.Clear();
                outOps.Add(op);
                proccessed.Add(op.Id);
                continue;
            }


            if (!ManageAddDependency(dependencyToWaiting, waitingOnDependencies, proccessed, op))
                continue;



            outOps.Add(op);
            proccessed.Add(op.Id);


            FulfillDependencies(dependencyToWaiting, waitingOnDependencies, proccessed, outOps, op);

        }

#if DEBUG

        if (waitingOnDependencies.Count > 0)
        {
            var aa = outOps.Select(x => $"({x.Id}){x.Type.Operator}").Join(",");
            var bb = waitingOnDependencies.SelectMany(x => x.Value.Item1).ToHashSet().Select(x => data.Ops[x]).Select(x => $"({x.Id}){x.Type.Operator}").Join(",");
            throw new QueryCompileException(waitingOnDependencies.Select(x => x.Value.Item2).ToList(), "Could not resolve dependencies");
        }

#endif

        ops = outOps;
    }


    private static bool ManageAddDependency(Dictionary<int, List<int>> dependencyToWaiting, Dictionary<int, (HashSet<int>, RawOp)> waitingOnDependencies, HashSet<int> proccessed, RawOp op)
    {
        var dependencies = new HashSet<int>();
        foreach (var x in op.GetInput())
        {
            if (!proccessed.Contains(x.Id))
                dependencies.Add(x.Id);
        }

        if (dependencies.Count == 0)
            return true;

        waitingOnDependencies.Add(op.Id, (dependencies, op));
        foreach (var x in dependencies)
        {
            if (!dependencyToWaiting.TryGetValue(x, out var waiting))
            {
                waiting = new List<int>();
                dependencyToWaiting[x] = waiting;
            }
            waiting.Add(op.Id);
        }

        return false;
    }


    private static void FulfillDependencies(Dictionary<int, List<int>> dependencyToWaiting, Dictionary<int, (HashSet<int>, RawOp)> waitingOnDependencies, HashSet<int> proccessed, List<RawOp> outOps,RawOp op)
    {
        var completed = new List<int> { op.Id };

        while (completed.Count > 0)
        {
            var id = completed[0];
            completed.RemoveAt(0);

            if (!dependencyToWaiting.TryGetValue(id, out var waiting2))
                continue;


            var removes = new List<int>();
            foreach (var waitingId in waiting2)
            {
                var dep = waitingOnDependencies[waitingId];
                dep.Item1.Remove(id);

                if (dep.Item1.Count == 0)
                {
                    waitingOnDependencies.Remove(waitingId);
                    outOps.Add(dep.Item2);
                    proccessed.Add(dep.Item2.Id);
                    completed.Add(waitingId);
                    removes.Add(waitingId);
                }
            }
            foreach (var r in removes)
                waiting2.Remove(r);

        }
    }


    private class SequenceProccessData
    {
        internal Dictionary<int, RawOp> Ops { get; private set; } = new Dictionary<int, RawOp>();
        internal Dictionary<int, List<int>> Groups { get; set; } = new Dictionary<int, List<int>>();
        internal Dictionary<int, int> Parents { get; set; } = new Dictionary<int, int>();
        internal int GetIndex(RawOp op)
        {
            var parentId = Parents[op.Id];
            var group = Groups[parentId];
            for (var i = 0; i < group.Count; i++)
            {
                if (group[i] == op.Id)
                    return i;
            }
            throw new QueryCompileException(op, $"Index not found in {parentId} [{group.Select(x => x.ToString()).Join(", ")}]");
        }


        internal void SequenceAffixes(RawOp op)
        {
            var post = op.IsPostfix();
            var pre = op.IsPrefix();

            if (!post && !pre)
                return;

            var parentId = Parents[op.Id];
            var parent = Ops[parentId];
            var group = Groups[parentId];

            if (post)
            {
                RawOp? target = null;
                var targetIndex = -1;
                for (var i = GetIndex(op) - 1; i >= 0; i--)
                {
                    var candidate = Ops[group[i]];
                    if (candidate.Type.Category.Has(OpCategory.Branching))
                        continue;
                    target = candidate;
                    targetIndex = i;
                    break;
                }


                if (targetIndex != -1)
                {
                    AddInput(parentId, targetIndex, op, false);
                }
                else if (parent.Type.Category.Has(OpCategory.Group))
                {
                    //groups will be untangled later
                    op.LeftInput.Add(parent);
                    op.PostFixed = true;
                }
                else
                {
                    throw new QueryCompileException(op, $"Postfix operation missing param");
                }
            }

#if DEBUG
            var sss = "";
            foreach (var x in Ops)
            {
                var o = x.Value;
                sss += $"\n{(o.Accessor == null ? o.Type.Operator : $"'{o.Accessor}'"),5} | pre {o.Prefixed,5} | post {o.PostFixed,5} | {o.Id} <- [{o.GetInput().Select(y => y.Id.ToString()).Join(",")}]";
            }
#endif


            if (pre)
            {
                RawOp? target = null;
                var targetIndex = -1;
                for (var i = GetIndex(op) + 1; i < group.Count; i++)
                {
                    var candidate = Ops[group[i]];
                    if (candidate.Type.Category.Has(OpCategory.Branching))
                        continue;
                    target = candidate;
                    targetIndex = i;
                    break;
                }

                if (targetIndex == -1)
                    throw new QueryCompileException(op, $"Sequence cannot end with prefix operators");

                AddInput(parentId, targetIndex, op, true);
            }
        }

        private void AddInput(int sourceParentId, int index, RawOp target, bool prefix)
        {

            var g = Groups[sourceParentId];
            var id = g[index];
            var op = Ops[id];

            if (target.Type.Category.Has(OpCategory.Branching))
            {
                if (prefix)
                    target.RightInput.Add(op);
                else
                    target.LeftInput.Add(op);
                return;
            }

            Parents[id] = target.Id;
            g.RemoveAt(index);

            if (!Groups.TryGetValue(target.Id, out var tg))
            {
                tg = [];
                Groups[target.Id] = tg;
            }


            if (prefix)
            {
                //tg.Add(id);
                target.Prefixed = true;
                target.RightInput.Add(op);
            }
            else
            {
                //tg.Insert(0, id);
                target.PostFixed = true;
                target.LeftInput.Add(op);
            }
        }
    }

    private void GroupOps(SequenceProccessData data, List<int> entries)
    {
        //var groups = new Dictionary<int, List<int>>();
        data.Groups[RootOpId] = [];
        var stack = new List<int> { RootOpId };
        foreach (var id in entries)
        {
            if (id == RootOpId)
                continue;

            var op = data.Ops[id];
            var parentId = stack[stack.Count - 1];

            if (parentId >= 0 && op.Type.Operator == data.Ops[parentId].Type.GroupOperator)
            {
                stack.RemoveAt(stack.Count - 1);
                continue;
            }

            var group = data.Groups[parentId];
            data.Parents[op.Id] = parentId;
            group.Add(op.Id);

            if (op.Type.Category.Has(OpCategory.Group))
            {
                stack.Add(op.Id);
                data.Groups.Add(op.Id, []);
            }
        }
    }


    private void RemapInput(SequenceProccessData data, RawOp op)
    {
        if (op.Type.Category.Has(OpCategory.Group) || op.Id == RootOpId)
            return;


        var replacements = new List<(bool, int, RawOp)>();

        for (var i = 0; i < op.LeftInput.Count; i++)
        {
            var x = op.LeftInput[i];
            if (!x.Type.Category.Has(OpCategory.Group) || x.LeftInput.Count == 0)
                continue;
            x = x.LeftInput.First();
            while (x != null && x.Type.Category.Has(OpCategory.Group))
                x = x.LeftInput.First();

            if (x == null)
                throw new QueryCompileException(op, "No valid input from group");

            if (x != null)
                replacements.Add((true, i, x));
        }



        foreach(var (left, i, newOp) in replacements)
        {
            RawOp old;

            if (left)
            {
                old = op.LeftInput[i];
                op.LeftInput[i] = newOp;
            }
            else
            {
                old = op.RightInput[i];
                op.RightInput[i] = newOp;
            }

            old.Output.Remove(op);
            newOp.Output.Add(op);

        }

    }


    private void RemapBranchingGroupsInput(SequenceProccessData data, RawOp op)
    {
        if (!op.Type.Category.Has(OpCategory.Group | OpCategory.Branching))
            return;

        var children = data.Groups[op.Id];

        if (children.Count == 0)
            return;

        var c = data.Ops[children.Last()];

        op.LeftInput.Clear();
        op.LeftInput.Add(c);

    }
}