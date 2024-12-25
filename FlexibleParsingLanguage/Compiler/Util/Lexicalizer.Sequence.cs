using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
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
        SequenceAffixes(data, ref ops);


        foreach (var op in ops)
        {
            foreach (var op2 in op.GetInput())
            {
                op2.Output.Add(op);
            }
        }

        RemapBranchingInputs(data, ref ops);

        foreach (var op in ops.Where(x => x.Type.Category.All(OpCategory.ParentInput)))
            AddParentInput(data, op);


        DisolveVirtuals(data, ref ops);

        SequenceDependencies(data, ref ops);
    }

    private class SequenceProccessData
    {
        internal Dictionary<int, RawOp> Ops { get; private set; } = new Dictionary<int, RawOp>();
        internal Dictionary<int, List<int>> Groups { get; set; } = new Dictionary<int, List<int>>();
        internal Dictionary<int, int> Parents { get; set; } = new Dictionary<int, int>();
        internal int GetIndex(RawOp op) => Lexicalizer.GetIndex(Parents, Groups, op);

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
                    if (candidate.Type.Category.All(OpCategory.Branching))
                        continue;
                    target = candidate;
                    targetIndex = i;
                    break;
                }


                if (targetIndex != -1)
                {
                    AddInput(parentId, targetIndex, op, false);
                }
                else if (parent.Type.Category.All(OpCategory.Group))
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
                sss += $"\n{o.Id,2} {(o.Accessor == null ? o.Type.Operator : $"'{o.Accessor}'"),5} | pre {o.Prefixed,5} | post {o.PostFixed,5} | [{o.GetInput().Select(y => y.Id.ToString()).Join(",")}] {(x.Value == op ? " <- " : null)}";
            }
#endif


            if (pre)
            {
                RawOp? target = null;
                var targetIndex = -1;
                for (var i = GetIndex(op) + 1; i < group.Count; i++)
                {
                    var candidate = Ops[group[i]];
                    if (candidate.Type.Category.All(OpCategory.Branching))
                        continue;
                    target = candidate;
                    targetIndex = i;
                    break;
                }

                if (targetIndex == -1)
                    throw new QueryCompileException(op, $"Prefix operator lacks input");

                AddInput(parentId, targetIndex, op, true);
            }
        }

        private void AddInput(int sourceParentId, int index, RawOp target, bool prefix)
        {

            var g = Groups[sourceParentId];
            var id = g[index];
            var op = Ops[id];

            if (target.Type.Category.All(OpCategory.Branching))
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
        var stack = new List<int> { };
        foreach (var id in entries)
        {
            var op = data.Ops[id];

            if (stack.Count > 0)
            {
                var parentId = stack[stack.Count - 1];
                if (parentId >= 0 && op.Type.Operator == data.Ops[parentId].Type.GroupOperator)
                {
                    stack.RemoveAt(stack.Count - 1);
                    continue;
                }
                var group = data.Groups[parentId];
                data.Parents[op.Id] = parentId;
                group.Add(op.Id);
            }

            if (op.Type.Category.All(OpCategory.Group))
            {


                stack.Add(op.Id);
                data.Groups.Add(op.Id, []);
            }
        }


#if DEBUG
        return;

        var ssss = entries.Select(x =>
        {
            var t = data.Ops[x].ToString();
            if (data.Parents.TryGetValue(x, out var v))
                t += " -> " + data.Parents[x]
;           return t;
        }).Join("\n");



        Action<int, int> pg = null;

        var log = new StringBuilder();
        var printGroup = (int depth, int id) =>
        {



            log.Append("\n");
            log.Append(new string(' ', depth * 4));

            if (depth > 10)
            {
                log.Append("***");
                return;
            }

            var op = data.Ops[id];
            log.Append(op.ToString());
            if (data.Groups.TryGetValue(id, out var g))
            {
                foreach (var e in g)
                {
                    pg(depth + 1, e);
                }
            }

        };
        pg = printGroup;

        foreach (var id in data.Groups.Where(x => !data.Parents.ContainsKey(x.Key)))
        {
            printGroup(0, id.Key);
        }

        var ss = log.ToString();

        var s = 3455;

#endif



    }

    private void SequenceAffixes(SequenceProccessData data, ref List<RawOp> ops)
    {
        var ordered = new List<(RawOp, int)>();

        ops.Select(x => x).ToList();


        for (var i = 0; i < ops.Count; i++)
            ordered.Add((ops[i], i));

        ordered.OrderByDescending(x => x.Item1.Type.Rank).ThenBy(x => x.Item2);

        var affixGroups = data.Groups.ToDictionary(x => x.Key, x => x.Value.ToList());
        var affixParents = data.Parents.ToDictionary(x => x.Key, x => x.Value);

        foreach (var op in ordered)
            SequenceAffixesInner(data, affixParents, affixGroups, op.Item1);
    }

    private void SequenceAffixesInner(SequenceProccessData data, Dictionary<int, int> parents, Dictionary<int, List<int>> children, RawOp op)
    {
        var post = op.IsPostfix();
        var pre = op.IsPrefix();

        if (!post && !pre)
            return;

        var parentId = parents[op.Id];
        var parent = data.Ops[parentId];
        var group = children[parentId];

        if (post)
        {
            RawOp? target = null;
            var targetIndex = -1;


            for (var i = GetIndex(parents, children, op) - 1; i >= 0; i--)
            {
                var candidate = data.Ops[group[i]];
                if (candidate.Type.Category.All(OpCategory.Branching))
                    continue;
                target = candidate;
                targetIndex = i;
                break;
            }


            if (targetIndex != -1)
            {
                AddInput(data, parents, children, parentId, targetIndex, op, false);
            }
            else if (parent.Type.Category.All(OpCategory.Group))
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
        foreach (var x in data.Ops)
        {
            var o = x.Value;
            sss += $"\n{o.Id,2} {(o.Accessor == null ? o.Type.Operator : $"'{o.Accessor}'"),5} | pre {o.Prefixed,5} | post {o.PostFixed,5} | [{o.GetInput().Select(y => y.Id.ToString()).Join(",")}] {(x.Value == op ? " <- " : null)}";
        }
#endif


        if (pre)
        {
            RawOp? target = null;
            var targetIndex = -1;
            for (var i = GetIndex(parents, children, op) + 1; i < group.Count; i++)
            {
                var candidate = data.Ops[group[i]];
                if (candidate.Type.Category.All(OpCategory.Branching))
                    continue;
                target = candidate;
                targetIndex = i;
                break;
            }

            if (targetIndex == -1)
                throw new QueryCompileException(op, $"Prefix operator lacks input");

            AddInput(data, parents, children, parentId, targetIndex, op, true);
        }
    }



    private void AddInput(SequenceProccessData data, Dictionary<int, int> parents, Dictionary<int, List<int>> children, int sourceParentId, int index, RawOp target, bool prefix)
    {

        var g = children[sourceParentId];
        var id = g[index];
        var op = data.Ops[id];

        if (target.Type.Category.All(OpCategory.Branching))
        {
            if (prefix)
                target.RightInput.Add(op);
            else
                target.LeftInput.Add(op);
            return;
        }

        parents[id] = target.Id;
        g.RemoveAt(index);

        if (!children.TryGetValue(target.Id, out var tg))
        {
            tg = [];
            children[target.Id] = tg;
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














    private void RemapToContextAtStartOfGroupfdesfd(SequenceProccessData data, RawOp op)
    {


        var ancestorId = data.Parents[op.Id];

        RawOp? target = null;


        if (op.Type.Category.All(OpCategory.ParentInput))
            return;

        var found = -1;


        var active = op;

        while (true)
        {
            var parentId = data.Parents[active.Id];
            var parent = data.Ops[parentId];

            if (!parent.Type.Category.All(OpCategory.ParentInput))
                    break;

            var group = data.Groups[parentId];

            for (var i = 0; i < group.Count; i++)
            {
                var id = group[i];

                if (id == op.Id)
                {
                    if (found < 0)
                        return;

        
                }

                var op2 = data.Ops[id];
            }
        }



    //.Where(x => x.Type.Category.Has(OpCategory.GroupContext))
    }


    private void AddParentInput(SequenceProccessData data, RawOp op)
    {
        RawOp? ctx = null;
        var ancestorId = data.Parents[op.Id];
        var ancestor = data.Ops[ancestorId];
        op.LeftInput.Add(ancestor);



        /*

        while (true)
        {
            var ancestor = data.Ops[ancestorId];
            if (ancestorId == RootGroupId)
            {
                ctx = ancestor;
                break;
            }

            if (ancestor.LeftInput.Count == 0 || ancestor.LeftInput[0] == null)
            {
                ancestorId = data.Parents[ancestor.Id];
                continue;
            }

            ctx = ancestor.LeftInput[0];

            if (ctx.Type.Category.Has(OpCategory.Group))
            {
                ancestorId = data.Parents[ctx.Id];
                continue;
            }


            break;
        }
                op.LeftInput.Add(ctx);
        */


        /*
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
        */
    }


    private void RemapBranchingInputs(SequenceProccessData data, ref List<RawOp> ops)
    {
        var proccessed = new HashSet<int>();

        foreach (var op in ops)
        {
            if (!op.Type.Category.All(OpCategory.Branching) || proccessed.Contains(op.Id))
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
                    active = active.LeftInput[0];
                else if (data.Parents.TryGetValue(active.Id, out var pId))
                    active = data.Ops[pId];
                else
                    target = active;

                if (!active.Type.Category.All(OpCategory.Branching))
                {
                    target = active;
                    break;
                }

                if (startId == active.Id)
                    throw new QueryCompileException(active, "branching loop", true);

                remaps.Add(active);
            }

            foreach (var r in remaps)
            {
                proccessed.Add(r.Id);
                r.LeftInput.Clear();
                if (target != null)
                    r.LeftInput.Add(target);
            }

            /*



            proccessed.Add(op.Id);

            RawOp? target = null;
          
            while (true)
            {
                var parentId = data.Parents[active.Id];

                var group = data.Groups[parentId];
                var foundId = parentId;

                for (var i = 0; i < group.Count; i++)
                {
                    var entryId = group[i];

                    if (entryId == active.Id)
                        break;

                    var entry = data.Ops[entryId];

                    if (entry.Type.Category.Any(OpCategory.Branching | OpCategory.UnGroup))
                        continue;

                    target = entry;
                }

                if (target != null)
                    break;

                active = data.Ops[parentId];

                if (parentId == RootGroupId)
                {
                    target = data.Ops[parentId];
                    break;
                }




            }

            foreach (var r in remaps)
            {

            }
            */



        }




        /*
                foreach (var op in ops.Where(op => op.Type.Category.All(OpCategory.Branching)))
 

 
        while (true)
        {
            ancestorId = data.Parents[active.Id];
            if (ancestorId == RootGroupId)
            {
                target = data.Ops[ancestorId];
                break;
            }

            var group = data.Groups[ancestorId];

            for (var i = 0; i < group.Count; i++)
            {
                var entryId = group[i];

                if (entryId == active.Id)
                    break;

                var entry = data.Ops[entryId];

                if (entry.Type.Category.Any(OpCategory.Branching | OpCategory.UnGroup))
                    continue;

                target = entry;
            }

            if (target != null)
            {
                if (!target.Type.Category.All(OpCategory.Group))
                    break;
                target = null;
            }

            active = data.Ops[ancestorId];
        }

        op.LeftInput.Add(target);







        var children = data.Groups[op.Id];

        if (children.Count == 0)
            return;

        var c = data.Ops[children.Last()];

        op.LeftInput.Clear();
        op.LeftInput.Add(c);
               */
    }



    private void DisolveVirtuals(SequenceProccessData data, ref List<RawOp> ops)
    {

#if DEBUG
        var before = ops.Where(x => !x.IsSimple()).Select(x => x.ToString()).Join("\n");
#endif



        var removes = new List<int>();


        for (int i = 0; i < ops.Count; i++) {
            var op = ops[i];

            if (!op.Type.Category.All(OpCategory.Virtual))
                continue;

            if (op.LeftInput.Count + op.RightInput.Count > 1)
                throw new QueryCompileException(op, "Virtual operators must have one or zero inputs", true);

            removes.Add(i);

            RawOp input;
            if (op.LeftInput.Count > 0)
                input = op.LeftInput[0];
            else if (op.RightInput.Count > 0)
                input = op.RightInput[0];
            else
                continue;

            foreach (var o in op.Output) {
                while (true)
                {
                    var j = o.LeftInput.IndexOf(op);
                    if (j < 0)
                        break;
                    o.LeftInput[j] = input;
                    break;
                }
                while (true)
                {
                    var j = o.RightInput.IndexOf(op);
                    if (j < 0)
                        break;
                    o.RightInput[j] = input;
                    break;
                }
            }
        }



#if DEBUG
        var after = ops.Where(x => !x.IsSimple()).Select(x => x.ToString()).Join("\n");
#endif


        removes.Reverse();

        foreach (var i in removes) {
            ops.RemoveAt(i);
        }

    }


    private void SequenceDependencies(SequenceProccessData data, ref List<RawOp> ops)
    {


        var proccessed = new HashSet<int>();
        var dependencyToWaiting = new Dictionary<int, List<int>>();
        var waitingOnDependencies = new Dictionary<int, (HashSet<int>, RawOp)>();

        var outOps = new List<RawOp>(ops.Count);
        foreach (var op in ops)
        {
            if ((op.Type.Category & (OpCategory.Virtual | OpCategory.UnGroup)) > 0 || op.Id == RootGroupId)
                continue;

            if (op.Id == RootGroupId)
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

        if (waitingOnDependencies.Count > 0)
        {
#if DEBUG
            var aaa = ops.RawQueryToString();
            var aab = outOps.RawQueryToString();

            var aa = outOps.Select(x => $"({x.Id}){x.Type.Operator}").Join(",");
            var bb = waitingOnDependencies.SelectMany(x => x.Value.Item1).ToHashSet().Select(x => data.Ops[x]).Select(x => $"({x.Id}){x.Type.Operator}").Join(",");
            var dsffsd = 543354;
#endif
            throw new QueryCompileException(waitingOnDependencies.Select(x => x.Value.Item2).ToList(), "Could not resolve dependencies", true);
        }
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



    private static int GetIndex(Dictionary<int, int> parents, Dictionary<int, List<int>> children, RawOp op)
    {
        var parentId = parents[op.Id];
        var group = children[parentId];
        for (var i = 0; i < group.Count; i++)
        {
            if (group[i] == op.Id)
                return i;
        }
        throw new QueryCompileException(op, $"Index not found in {parentId} [{group.Select(x => x.ToString()).Join(", ")}]");
    }
}