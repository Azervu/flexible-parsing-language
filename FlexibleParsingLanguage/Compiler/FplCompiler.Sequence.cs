using FlexibleParsingLanguage.Operations;
using System.Text;

namespace FlexibleParsingLanguage.Compiler;

public partial class FplCompiler
{

    private enum PrefixType : byte
    {
        Left = 0,
        Right = 1
    }

    internal void Sequence(SequenceProccessData data, ref List<RawOp> ops)
    {
        data.Ops = ops.ToDictionary(x => x.Id, x => x);


#if DEBUG
        var a = DebugSequence(ops);
#endif

        GroupOps(data, ref ops);


        foreach (var op in ops)
        {
            foreach (var op2 in op.GetRawInput())
                op2.Output.Add(op);
        }
#if DEBUG
        var b = DebugSequence(ops);
#endif


        SequenceAffixes(data, ref ops);

        foreach (var op in ops)
        {
            foreach (var op2 in op.GetRawInput())
                op2.Output.Add(op);
        }

        RemapGroupInputHierarchy(data, ref ops);



        data.ActiveWriteId = ReadRootId;
        data.ActiveReadId = WriteRootId;
        foreach (var op in ops)
        {
            if (op.Type.Sequence != null)
                op.Type.Sequence(data, op);

            if (op.ReadId == -1)
                op.ReadId = data.ActiveReadId;

            if (op.WriteId == -1)
                op.WriteId = data.ActiveWriteId;
        }

#if DEBUG
        var c = DebugSequence(ops);
#endif

        foreach (var op in ops)
        {
            if (op.Type.SequenceType.All(OpSequenceType.Group))
            {
                op.LeftInput.Clear();
                foreach (var children in data.Ops[op.Id].AffixChildren)
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

#if DEBUG
        var d = DebugSequence(ops);
#endif

        DissolveVirtuals(data, ref ops);

#if DEBUG
        var e = DebugSequence(ops);
#endif

        SequenceDependencies(data, ref ops);

        foreach (var op in ops)
            op.Input = op.GetRawInput().ToList();


#if DEBUG
        var g = DebugSequence(ops);
        var s = 34564365;
#endif
    }

    private void GroupOps(SequenceProccessData data, ref List<RawOp> ops)
    {
        var stack = new List<(int Id, int Index)> {
            (RootGroupId, 0)
        };
        data.GroupParents[RootGroupId] = (RootGroupId, 0);

        data.Ops[RootGroupId] = new RawOp()
        {
            Id = RootGroupId,
            Type = Branch,
            GroupChildren = [[]]
        };

        foreach (var op in ops)
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
                data.Ops[parentId].GroupChildren.Add([]);
                continue;
            }

            var group = data.Ops[parentId].GroupChildren[i];
            data.GroupParents[op.Id] = (parentId, i);
            group.Add(op.Id);

            if (op.Type.SequenceType.All(OpSequenceType.Group))
            {
                stack.Add((op.Id, 0));
                data.Ops[op.Id].GroupChildren = [[]];
            }
        }
        ops = ops.Where(x => !x.Type.SequenceType.Any(OpSequenceType.UnGroup | OpSequenceType.GroupSeparator)).ToList();
    }

    private void SequenceAffixes(SequenceProccessData data, ref List<RawOp> ops)
    {
        data.AffixParents = data.GroupParents.ToDictionary(x => x.Key, x => x.Value);

        foreach (var op in data.Ops.Where(x => x.Value.GroupChildren.Count > 0))
            op.Value.AffixChildren = op.Value.GroupChildren.ToList();

        foreach (var op in ops)
        {
            SequenceAffixesInner(data, op);
        }
    }

    private void SequenceAffixesInner(SequenceProccessData data, RawOp op)
    {

        if (!data.AffixParents.TryGetValue(op.Id, out var x))
            return;

        var post = op.IsPostfix();
        var pre = op.IsPrefix();
        var opt = op.IsOptFix();

        if (!post && !pre && !opt && !op.Type.SequenceType.All(OpSequenceType.Named))
            return;

        var parentId = x.ParentId;
        var parent = data.Ops[parentId];
        var parentChildren = parent.AffixChildren[x.Index];

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
                AddInput(data, parentChildren, targetIndex, op, PrefixType.Left);
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

                if (!candidate.Type.SequenceType.All(OpSequenceType.Accessor))
                {
                    targetIndex = -2;
                    break;
                }
                target = candidate;
                targetIndex = i;
                break;
            }

            if (targetIndex > 0)
            {
                AddInput(data, parentChildren, targetIndex, op, PrefixType.Right);
            }
            else if (parent.Type.SequenceType.All(OpSequenceType.Branching | OpSequenceType.RightInput))
            {
                op.RightInput.Add(parent);
                op.PostFixed = true;
            }
            else
            {
                //throw new QueryException(op, $"Prefix operator lacks input");
            }
        }

        if (opt)
        {
            RawOp? target = null;
            var targetIndex = -1;
            var index = parentChildren.IndexOf(op.Id);
            if (index >= 0)
            {
                for (var i = index + 1; i < parentChildren.Count; i++)
                {
                    var candidate = data.Ops[parentChildren[i]];
                    if (candidate.Type.SequenceType.All(OpSequenceType.Branching))
                        continue;

                    if (!candidate.Type.SequenceType.All(OpSequenceType.Group))
                        break;

                    target = candidate;
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex != -1)
                AddInput(data, parentChildren, targetIndex, op, PrefixType.Right);
        }
    }

    private void AddInput(SequenceProccessData data, List<int> sourceChildren, int sourceIndex, RawOp target, PrefixType prefixType)
    {
#if DEBUG
        if (sourceIndex >= sourceChildren.Count)
            throw new Exception($"Invalid index {sourceIndex}");

#endif


        var id = sourceChildren[sourceIndex];
        var op = data.Ops[id];

#if DEBUG
        var s = target.LeftInput;

        if (op == null)
            throw new Exception($"A------------{id}");
#endif

        if (target.Type.SequenceType.All(OpSequenceType.Branching))
        {
            if (prefixType == PrefixType.Left)
                target.LeftInput.Add(op);
            else
                target.RightInput.Add(op);
            return;
        }

#if DEBUG
        s = target.LeftInput;
        if (op == null)
            throw new Exception($"B------------{id}");
#endif

        sourceChildren.RemoveAt(sourceIndex);

        var targetChildren = data.Ops[target.Id].AffixChildren;
        data.AffixParents[id] = (target.Id, 0);

        if (targetChildren.Count == 0)
            targetChildren.Add([]);

#if DEBUG
        s = target.LeftInput;
        if (op == null)
            throw new Exception($"C------------{id}");
#endif

        switch (prefixType)
        {
            case PrefixType.Left:
                foreach (var tg in targetChildren)
                {
                    tg.Insert(0, id);
                    target.PostFixed = true;
                    target.LeftInput.Add(op);
                }
                break;
            case PrefixType.Right:
                foreach (var tg in targetChildren)
                {
                    tg.Add(id);
                    target.Prefixed = true;
                    target.RightInput.Add(op);
                }
                break;
        }

#if DEBUG
        try
        {
            s = target.LeftInput;
            if (op == null)
                throw new Exception($"D------------{id}");
        }
        catch(Exception ex)
        {
            var dsf = 345;
        }
#endif

        foreach (var input in target.LeftInput)
        {
            if (input.Type.SequenceType.All(OpSequenceType.Group) && data.AffixParents.TryGetValue(input.Id, out var inputParent) && target.Id == inputParent.ParentId)
                throw new QueryException(input, "Invalid postfix group", false);
        }

#if DEBUG
        s = target.LeftInput;
        if (op == null)
            throw new Exception($"E------------{id}");
#endif

    }

    private void DissolveVirtuals(SequenceProccessData data, ref List<RawOp> ops)
    {







#if DEBUG
        var before = ops.Where(x => !x.IsSimple()).Select(x => x.ToString()).Join("\n");
#endif
        var removes = new List<int>();












        /*

( 6/ 0)    $ | SequenceType = RootParam
( 5/ 0)    . | SequenceType = RightInput, LeftInput, Default | L=[6] | R=[4] | C=[6, 4]
( 4/ 0)      | SequenceType = Accessor
( 7/ 4)   @@ | SequenceType = LeftInput, Named, Branching, Virtual | L=[5]
( 8/ 7)    @ | SequenceType = Named, Virtual | L=[5]
(10/10)    . | SequenceType = RightInput, LeftInput, Default | L=[8] | R=[9] | C=[8, 9]
( 9/10)      | SequenceType = Accessor
(11/11)    : | SequenceType = RightInput, LeftInput | L=[10] | R=[12] | C=[10, 12]
(12/12)      | SequenceType = Accessor
(13/14)    @ | SequenceType = Named, Virtual | L=[5]
(14/16)   @@ | SequenceType = LeftInput, Named, Branching, Virtual | L=[13]
(15/19)    @ | SequenceType = Named, Virtual | L=[13]
(17/22)    . | SequenceType = RightInput, LeftInput, Default | L=[15] | R=[16] | C=[15, 16]
(16/22)      | SequenceType = Accessor
(18/23)    : | SequenceType = RightInput, LeftInput | L=[17] | R=[19] | C=[17, 19]
(19/24)      | SequenceType = Accessor
(20/26)    @ | SequenceType = Named, Virtual | L=[13]
(22/28)    . | SequenceType = RightInput, LeftInput, Default | L=[20] | R=[21] | C=[20, 21]
(21/28)      | SequenceType = Accessor
(23/30)    : | SequenceType = RightInput, LeftInput | L=[22] | R=[24] | C=[22, 24]
(24/31)      | SequenceType = Accessor


( 6/ 0)    $ | SequenceType = RootParam
( 5/ 0)    . | SequenceType = RightInput, LeftInput, Default | L=[6] | R=[4] | C=[6, 4]
( 4/ 0)      | SequenceType = Accessor
(10/10)    . | SequenceType = RightInput, LeftInput, Default | L=[5] | R=[9] | C=[8, 9]
( 9/10)      | SequenceType = Accessor
(11/11)    : | SequenceType = RightInput, LeftInput | L=[10] | R=[12] | C=[10, 12]
(12/12)      | SequenceType = Accessor
(17/22)    . | SequenceType = RightInput, LeftInput, Default | L=[13] | R=[16] | C=[15, 16]
(16/22)      | SequenceType = Accessor
(18/23)    : | SequenceType = RightInput, LeftInput | L=[17] | R=[19] | C=[17, 19]
(19/24)      | SequenceType = Accessor
(22/28)    . | SequenceType = RightInput, LeftInput, Default | L=[13] | R=[21] | C=[20, 21]
(21/28)      | SequenceType = Accessor
(23/30)    : | SequenceType = RightInput, LeftInput | L=[22] | R=[24] | C=[22, 24]
(24/31)      | SequenceType = Accessor


        */


        var remapping = new Dictionary<int, List<RawOp>>();


        foreach(var op in ops.Where(x => x.Type.SequenceType.All(OpSequenceType.Virtual)))
        {
            if (remapping.ContainsKey(op.Id))
                continue;

            var current = op.GetRawInput().ToList();
            var notConfirmedVirtuals = true;
            while (notConfirmedVirtuals)
            {
                var next = new List<RawOp>();
                notConfirmedVirtuals = false;
                foreach (var c in current)
                {
                    if (remapping.TryGetValue(c.Id, out var x))
                    {
                        next.AddRange(x);
                    }
                    else if (c.Type.SequenceType.All(OpSequenceType.Virtual))
                    {
                        notConfirmedVirtuals = true;
                        next.AddRange(op.GetRawInput());
                    }
                    else
                    {
                        next.Add(c);
                    }
                }
                current = next;
            }

            remapping[op.Id] = current;
        }

        foreach (var op in ops.Where(x => !x.Type.SequenceType.All(OpSequenceType.Virtual)))
        {
            for(var i = 0; i < op.LeftInput.Count; i++)
            {
                if (remapping.TryGetValue(op.LeftInput[i].Id, out var inserts)) {
                    op.LeftInput.RemoveAt(i);
                    op.LeftInput.InsertRange(i, inserts);
                }
            }
            for (var i = 0; i < op.RightInput.Count; i++)
            {
                if (remapping.TryGetValue(op.RightInput[i].Id, out var inserts))
                {
                    op.RightInput.RemoveAt(i);
                    op.RightInput.InsertRange(i, inserts);
                }
            }
        }



        /*
        for (int i = 0; i < ops.Count; i++)
        {
            var op = ops[i];

            if (!op.Type.SequenceType.All(OpSequenceType.Virtual))
                continue;

            var inputs = op.GetRawInput().ToList();
            removes.Add(i);


#if DEBUG
            if (op.Id == 13)
            {
                var s2 = 534646;
            }

            if (inputs.Where(x => x == null).Any())
            {
                var s3 = 5464;
            }
#endif

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
        */

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
        var completed = new List<int> { op.Id, WriteRootId };

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

#if DEBUG
    private string DebugSequence(List<RawOp> ops)
    {
        return ops.Select(x => {
            var log = new StringBuilder($"({x.Id,2}/{x.CharIndex,2}){(x.Type.SequenceType.Any(OpSequenceType.Literal) ? $"'{x.Accessor}'" : x.Type.Operator),5} | SequenceType = {x.Type.SequenceType}");

            if (x.LeftInput.Count > 0)
                log.Append($" | L=[{string.Join(", ", x.LeftInput.Select(x => x.Id))}]");

            if (x.RightInput.Count > 0)
                log.Append($" | R=[{string.Join(", ", x.RightInput.Select(x => x.Id))}]");

            foreach (var c in x.AffixChildren)
                log.Append($" | C=[{string.Join(", ", c)}]");

            if (x.Output.Count > 0)
                log.Append($" | OUT=[{string.Join(", ", x.Output.Select(x => x.Id))}]");


            return log.ToString();
        }).Join("\n"); ;
    }
#endif

}