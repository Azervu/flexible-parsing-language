using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FlexibleParsingLanguage.Compiler;

internal partial class Compiler
{

    private class SequenceProccessData
    {
        internal Dictionary<int, RawOp> Ops { get; set; }

        internal Dictionary<int, (int ParentId, int Index)> GroupParents { get; set; } = new Dictionary<int, (int ParentId, int Index)>();
        internal Dictionary<int, List<List<int>>> GroupChildren { get; set; } = new Dictionary<int, List<List<int>>>();


        internal Dictionary<int, (int ParentId, int Index)> AffixParents { get; set; } = new Dictionary<int, (int ParentId, int Index)>();
        internal Dictionary<int, List<List<int>>> AffixChildren { get; set; } = new Dictionary<int, List<List<int>>>();


        internal int GetAffixIndex(RawOp op)
        {
            var (parentId, groupIndex) = AffixParents[op.Id];
            var children = AffixChildren[parentId][groupIndex];
            var i = children.IndexOf(op.Id);
            if (i >= 0)
                return i;
            throw new QueryException(op, $"Index not found in ({parentId}, {groupIndex}) [{children.Select(x => x.ToString()).Join(", ")}]");
        }
    }


    internal void Sequence(ref List<RawOp> ops)
    {
        var data = new SequenceProccessData();
        data.Ops = ops.ToDictionary(x => x.Id, x => x);

        GroupOps(data, ref ops);
        SequenceAffixes(data, ref ops);


        foreach (var op in ops)
        {
            foreach (var op2 in op.GetRawInput())
                op2.Output.Add(op);
        }

        RemapGroupInputHierarchy(data, ref ops);
        foreach (var op in ops.Where(x => x.Type.SequenceType.All(OpSequenceType.ParentInput)))
            AddParentInput(data, op);

        foreach (var op in ops)
        {
            if (op.Type.SequenceType.All(OpSequenceType.Group))
            {
                op.LeftInput.Clear();

                foreach (var children in data.AffixChildren[op.Id])
                {
                    for (int i = children.Count - 1; i >= 0; i--)
                    {
                        var t = data.Ops[children[i]];

                        if (!t.Type.SequenceType.All(OpSequenceType.Branching))
                        {
                            op.LeftInput.Add(t);
                            break;
                        }
                    }
                }
            }
        }

        DissolveVirtuals(data, ref ops);

        SequenceDependencies(data, ref ops);

        foreach (var op in ops)
            op.Input = op.GetRawInput().ToList();
    }

    private void GroupOps(SequenceProccessData data, ref List<RawOp> ops)
    {
        var stack = new List<(int Id, int Index)> { };
        foreach (var op in ops)
        {
            if (stack.Count > 0)
            {
                var (parentId, i) = stack[stack.Count - 1];
                if (parentId >= 0 && op.Type.Operator == data.Ops[parentId].Type.GroupOperator)
                {
                    stack.RemoveAt(stack.Count - 1);
                    continue;
                }

                if (op.Type.SequenceType.All(OpSequenceType.GroupSeparator))
                {
                    stack[stack.Count - 1] = (parentId, i + 1);
                    data.GroupChildren[parentId].Add([]);
                    continue;
                }


                var group = data.GroupChildren[parentId][i];
                data.GroupParents[op.Id] = (parentId, i);
                group.Add(op.Id);
            }

            if (op.Type.SequenceType.All(OpSequenceType.Group))
            {
                stack.Add((op.Id, 0));
                data.GroupChildren.Add(op.Id, [[]]);
            }
        }


        ops = ops.Where(x => !x.Type.SequenceType.All(OpSequenceType.UnGroup)).ToList();

        //ops = ops.Where(x => !x.Type.SequenceType.Any(OpSequenceType.UnGroup | OpSequenceType.GroupSeparator)).ToList();
    }

    private void SequenceAffixes(SequenceProccessData data, ref List<RawOp> ops)
    {
        var ordered = new List<(RawOp, int)>();

        ops.Select(x => x).ToList();


        for (var i = 0; i < ops.Count; i++)
            ordered.Add((ops[i], i));

        ordered.OrderByDescending(x => x.Item1.Type.Rank).ThenBy(x => x.Item2);

        data.AffixParents = data.GroupParents.ToDictionary(x => x.Key, x => x.Value);
        data.AffixChildren = data.GroupChildren.ToDictionary(x => x.Key, x => x.Value.ToList());

        foreach (var op in ordered)
        {
            SequenceAffixesInner(data, op.Item1);
        }

#if DEBUG
        var sss = "";
        foreach (var x in data.Ops)
        {
            var o = x.Value;
            sss += $"\n{o.Id,2} {(o.Accessor == null ? o.Type.Operator : $"'{o.Accessor}'"),5} | pre {o.Prefixed,5} | post {o.PostFixed,5} | [{o.GetRawInput().Select(y => y.Id.ToString()).Join(",")}]";
        }

        var debug = 543645;
#endif
    }

    private void SequenceAffixesInner(SequenceProccessData data, RawOp op)
    {
        if (!data.AffixParents.TryGetValue(op.Id, out var x))
            return;

        var post = op.IsPostfix();
        var pre = op.IsPrefix();

        if (!post && !pre)
            return;

        var parentId = x.ParentId;
        var parent = data.Ops[parentId];
        var parentChildren = data.AffixChildren[parentId][x.Index];


        if (post)
        {
            RawOp? target = null;
            var targetIndex = -1;
            var index = parentChildren.IndexOf(op.Id);

            if (index == -1)
                throw new QueryException(op, $"Index not found in ({parentId}, {x.Index}) [{parentChildren.Select(x => x.ToString()).Join(", ")}]");

            for (var i = index - 1; i >= 0; i--)
            {
                var candidate = data.Ops[parentChildren[i]];
                if (candidate.Type.SequenceType.All(OpSequenceType.Branching))
                    continue;
                target = candidate;
                targetIndex = i;
                break;
            }

            if (targetIndex != -1)
            {
                AddInput(data, parentChildren, targetIndex, op, false);
            }
            else if (parent.Type.SequenceType.All(OpSequenceType.Branching | OpSequenceType.LeftInput))
            {
                op.LeftInput.Add(parent);
                op.PostFixed = true;
            }
            else
            {
                throw new QueryException(op, $"Postfix operation missing param");
            }
        }

        if (pre)
        {
            RawOp? target = null;
            var targetIndex = -1;

            var index = parentChildren.IndexOf(op.Id);
            if (index == -1)
                throw new QueryException(op, $"Index not found in ({parentId}, {x.Index}) [{parentChildren.Select(x => x.ToString()).Join(", ")}]");


            for (var i = index + 1; i < parentChildren.Count; i++)
            {
                var candidate = data.Ops[parentChildren[i]];
                if (candidate.Type.SequenceType.All(OpSequenceType.Branching))
                    continue;
                target = candidate;
                targetIndex = i;
                break;
            }

            if (targetIndex != -1)
            {
                AddInput(data, parentChildren, targetIndex, op, true);
            }
            else if (parent.Type.SequenceType.All(OpSequenceType.Branching | OpSequenceType.RightInput))
            {
                op.RightInput.Add(parent);
                op.PostFixed = true;
            }
            else
            {
                throw new QueryException(op, $"Prefix operator lacks input");
            }
        }
    }



    private void AddInput(SequenceProccessData data, List<int> sourceChildren, int sourceIndex, RawOp target, bool prefix)
    {

        var id = sourceChildren[sourceIndex];
        var op = data.Ops[id];

        if (target.Type.SequenceType.All(OpSequenceType.Branching))
        {
            if (prefix)
                target.RightInput.Add(op);
            else
                target.LeftInput.Add(op);
            return;
        }

        sourceChildren.RemoveAt(sourceIndex);

        if (!data.AffixChildren.TryGetValue(target.Id, out var targetChildren))
        {
            targetChildren = [];
            data.AffixChildren[target.Id] = targetChildren;
        }
        data.AffixParents[id] = (target.Id, 0);

        if (targetChildren.Count == 0)
            targetChildren.Add([]);
        else if (targetChildren.Count() > 1)
            throw new Exception();

        var tg = targetChildren[0];

        if (prefix)
        {
            tg.Add(id);
            target.Prefixed = true;
            target.RightInput.Add(op);
        }
        else
        {
            tg.Insert(0, id);
            target.PostFixed = true;
            target.LeftInput.Add(op);
        }
    }


    private void AddParentInput(SequenceProccessData data, RawOp op)
    {
        RawOp? ctx = null;
        var (ancestorId, i) = data.GroupParents[op.Id];
        var ancestor = data.Ops[ancestorId];

        if (ancestor.LeftInput.Count >= 0)
            op.LeftInput.Add(ancestor.LeftInput[0]);
    }

    private void RemapGroupInputHierarchy(SequenceProccessData data, ref List<RawOp> ops)
    {
        var proccessed = new HashSet<int>();


        foreach (var op in ops)
        {
            if (!op.Type.SequenceType.All(OpSequenceType.Group) || proccessed.Contains(op.Id))
                continue;

            var active = op;
            RawOp? target = null;
            var remaps = new List<RawOp> { op };
            while (true)
            {
                if (proccessed.Contains(active.Id))
                {

                    target = active.LeftInput.Count > 0
                        ? active.LeftInput[0]
                        : null;
                    break;
                }

                var startId = active.Id;


                if (active.LeftInput.Count > 0)
                {
                    active = active.LeftInput[0];
                    if (!active.Type.SequenceType.All(OpSequenceType.Group))
                    {
                        target = active;
                        break;
                    }
                }
                else if (data.AffixParents.TryGetValue(active.Id, out var x))
                {
                    active = data.Ops[x.ParentId];
                }


                remaps.Add(active);

                if (active.Id == RootGroupId)
                {
                    target = active;
                    break;
                }
                else
                {

                    if (startId == active.Id)
                        throw new QueryException(active, "grouping loop", true);
                }
            }

            foreach (var r in remaps)
            {
                proccessed.Add(r.Id);
                r.LeftInput.Clear();
                if (target != null)
                    r.LeftInput.Add(target);


            }
        }
    }


    private void DissolveVirtuals(SequenceProccessData data, ref List<RawOp> ops)
    {

#if DEBUG
        var before = ops.Where(x => !x.IsSimple()).Select(x => x.ToString()).Join("\n");
#endif

        var removes = new List<int>();


        for (int i = 0; i < ops.Count; i++)
        {
            var op = ops[i];

            if (!op.Type.SequenceType.All(OpSequenceType.Virtual))
                continue;


            var inputs = op.GetRawInput().ToList();

            if (inputs.Count == 0)
                continue;

            removes.Add(i);

            foreach (var o in op.Output)
            {
                var j = o.LeftInput.IndexOf(op);
                if (j >= 0)
                {
                    o.LeftInput.RemoveAt(j);
                    o.LeftInput.InsertRange(j, inputs);
                }
                j = o.RightInput.IndexOf(op);
                if (j >= 0)
                {
                    o.RightInput.RemoveAt(j);
                    o.RightInput.InsertRange(j, inputs);
                }
            }
        }



        removes.Reverse();

        foreach (var i in removes)
        {
            ops.RemoveAt(i);
        }


#if DEBUG
        var after = ops.Where(x => !x.IsSimple()).Select(x => x.ToString()).Join("\n");
        var d = 354;
#endif




    }


    private void SequenceDependencies(SequenceProccessData data, ref List<RawOp> ops)
    {


        var proccessed = new HashSet<int>();
        var dependencyToWaiting = new Dictionary<int, List<int>>();
        var waitingOnDependencies = new Dictionary<int, (HashSet<int>, RawOp)>();

        var outOps = new List<RawOp>(ops.Count);
        foreach (var op in ops)
        {
            if ((op.Type.SequenceType & (OpSequenceType.Virtual | OpSequenceType.UnGroup | OpSequenceType.GroupSeparator)) > 0)
                continue;

            /*
            if (op.Id == RootGroupId)
            {
                op.LeftInput.Clear();
                op.RightInput.Clear();
                outOps.Add(op);
                proccessed.Add(op.Id);
                continue;
            }
            */


            if (!ManageAddDependency(dependencyToWaiting, waitingOnDependencies, proccessed, op))
                continue;

            outOps.Add(op);
            proccessed.Add(op.Id);


            FulfillDependencies(dependencyToWaiting, waitingOnDependencies, proccessed, outOps, op);

        }

        if (waitingOnDependencies.Count > 0)
        {
#if DEBUG
            var aaa = ops.RawQueryToString();
            var aab = outOps.RawQueryToString();

            var aa = outOps.Select(x => $"({x.Id}){x.Type.Operator}").Join(",");
            var bb = waitingOnDependencies.SelectMany(x => x.Value.Item1).ToHashSet().Select(x => data.Ops[x]).Select(x => $"({x.Id}){x.Type.Operator}").Join(",");
            var dsffsd = 543354;
#endif
            throw new QueryException(waitingOnDependencies.Select(x => x.Value.Item2).ToList(), "Could not resolve dependencies", true);
        }
        ops = outOps;

    }

    private static bool ManageAddDependency(Dictionary<int, List<int>> dependencyToWaiting, Dictionary<int, (HashSet<int>, RawOp)> waitingOnDependencies, HashSet<int> proccessed, RawOp op)
    {
        var dependencies = new HashSet<int>();
        foreach (var x in op.GetRawInput())
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
    private static void FulfillDependencies(Dictionary<int, List<int>> dependencyToWaiting, Dictionary<int, (HashSet<int>, RawOp)> waitingOnDependencies, HashSet<int> proccessed, List<RawOp> outOps, RawOp op)
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
}