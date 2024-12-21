using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler.Util;

internal partial class Lexicalizer
{

    internal class SequenceWrapper
    {
        internal int Id { get; set; }
        internal int ParentId { get; set; }


        internal int LeftIndex { get; set; }
        internal int RightIndex { get; set; }


        internal SequenceWrapper? Parent { get; set; }
        internal List<SequenceWrapper> Children { get; private set; } = new List<SequenceWrapper>();
        internal List<SequenceWrapper> Input { get; set; } = new List<SequenceWrapper>();
        internal List<SequenceWrapper> Output { get; set; } = new List<SequenceWrapper>();
        internal RawOp Op { get; set; }

        internal bool PrefixProccessed
        {
            get; private set;
        }

        internal bool PostfixProccessed
        {
            get; private set;
        }



        internal bool TryAddPrefix(SequenceWrapper sequence)
        {
            if (!Op.Type.Category.Has(OpCategory.Prefix) || PrefixProccessed)
                return false;
            PrefixProccessed = true;
            Input.Add(sequence);
            return true;
        }

        internal bool TryAddPostfix(SequenceWrapper sequence)
        {
            if (!Op.Type.Category.Has(OpCategory.Prefix) || PrefixProccessed)
                return false;
            PrefixProccessed = true;
            Input.Insert(0, sequence);
            return true;
        }


        internal bool IsBranch() => Op.Type.Category.Has(OpCategory.Branching);

        internal bool IsPrefix()
        {
            if (Op.Type.Category.Has(OpCategory.Prefix) && !PrefixProccessed)
                return true;

            return false;
        }

        internal bool IsPostfix()
        {
            if (Op.Type.Category.Has(OpCategory.Postfix) && !PostfixProccessed)
                return true;

            return false;
        }

        internal int PrefixRank()
        {
            if (IsPrefix())
                return Op.Type.Rank;
            return int.MinValue;
        }

        internal int PostfixRank()
        {
            if (IsPostfix())
                return Op.Type.Rank;
            return int.MinValue;
        }

        internal int Rank { get => Op.Type.Rank; }


    };








    internal class SequenceProccessData
    {
        internal Dictionary<int, SequenceWrapper> Ops { get; private set; } = new Dictionary<int, SequenceWrapper>();
        internal Dictionary<int, List<int>> Groups { get; set; } = new Dictionary<int, List<int>>();

        internal int GetIndex(SequenceWrapper op)
        {
            var parentId = Ops[op.Id].ParentId;
            var group = Groups[parentId];
            for (var i = 0; i < group.Count; i++)
            {
                if (group[i] == op.Id)
                    return i;
            }
            throw new Exception($"Index not found ({op.Id}) [{group.Select(x => x.ToString()).Join(", ")}]");
        }

        internal void Move(SequenceWrapper op, int parentId, bool toLeft)
        {
            var oldParentId = op.ParentId;
            var oldGroup = Groups[oldParentId];
            op.ParentId = parentId;
        }
    }


    internal void Sequence(ref List<RawOp> rawOps)
    {
        var data = new SequenceProccessData();
        var entries = new List<int>();


        for (var i = 0; i < rawOps.Count; i++)
        {
            var op = rawOps[i];
            op.Id = i;
            if (op.Type.Operator == DefaultOp.Operator)
            {
                if (op.Accessor != null)
                    throw new InvalidOperationException("Accessor on default");
                continue;
            }
            data.Ops.Add(i, new SequenceWrapper
            {
                Op = op,
                Id = i,
                ParentId = -1,
            });
            entries.Add(i);
        }



        data.Groups[-1] = [];
        var stack = new List<int> { -1 };
        foreach (var id in entries)
        {
            var op = data.Ops[id];
            var parentId = stack[stack.Count - 1];

            if (parentId >= 0 && op.Op.Type.Operator == data.Ops[parentId].Op.Type.GroupOperator.ToString())
            {
                stack.RemoveAt(stack.Count - 1);
                continue;
            }
            
            var group = data.Groups[parentId];
            op.ParentId = parentId;
            group.Add(op.Id);

            if (op.Op.Type.Category.Has(OpCategory.Group))
            {
                stack.Add(op.Id);
                data.Groups.Add(op.Id, []);
            }
        }

        var ordered = entries.Select(x => data.Ops[x]).ToList();
        ordered.OrderByDescending(x => x.Op.Type.Rank).ThenBy(x => x.Id);
        foreach (var op in ordered)
            HandleSequenceOp(data, op);


        //HandleAccessors(data);
        //SequenceInner(data);
    }

    private void HandleSequenceOp(SequenceProccessData data, SequenceWrapper op)
    {
        if (op.Op.Type.Operator == AccessorOp.Operator)
        {
            HandleAccessor(data, op);
            return;
        }

        if (op.Op.Type.Category.Has(OpCategory.Group))
        {
            HandleGroup(data, op);
            return;
        }

        HandleFix(data, op);
    }

    private void HandleAccessor(SequenceProccessData data, SequenceWrapper op)
    {
        SequenceWrapper? left = null;
        SequenceWrapper? right = null;

        var group = data.Groups[op.ParentId];

        var i = data.GetIndex(op);

        for (var l = i - 1; l >= 0; l--)
        {
            var lId = group[l];
            left = data.Ops[lId];

            if (left.IsBranch())
                continue;

            if (!left.IsPrefix())
                left = null;

            break;
        }

        for (var r = i + 1; r < group.Count; r++)
        {
            var rId = group[r];
            right = data.Ops[rId];

            if (!right.IsBranch())
                continue;

            if (!right.IsPostfix())
                right = null;

            break;
        }

        if (left == null && right == null)
        {
            op.Op.Type = DefaultOp;
            return;
        }

        if ((left?.Rank ?? int.MinValue) > (right?.Rank ?? int.MinValue))
            data.Move(op, left.Id, false);
        else
            data.Move(op, right.Id, true);
    }








    private void HandleGroup(SequenceProccessData data, SequenceWrapper op)
    {


        /*
        var ownGroup = data.Groups[op.Id];

        if (ownGroup.Count == 0)
            return;



        var i = data.GetIndex(op);

        var groupId = stack[stack.Count - 1];

        if (groupId >= 0 && op.Op.Type.Operator == opDict[groupId].Op.Type.GroupOperator.ToString())
        {
            stack.RemoveAt(stack.Count - 1);
            continue;
        }

        var group = groupMap[groupId];
        groupsInv[op.Id] = new SequenceIndex(groupId, group.Count);
        group.Add(op.Id);

        if (op.Op.Type.Category.Has(OpCategory.Group))
        {
            stack.Add(op.Id);
            groupMap.Add(op.Id, []);
        }

        */
    }















    private void HandleFix(SequenceProccessData data, SequenceWrapper op)
    {
        var group = data.Groups[op.ParentId];
        var index = data.GetIndex(op);

    }
















































    /*




    private void HandleAccessors(SequenceProccessData data)
    {
        var removes = new List<int>();

        var group = data.Groups[-1];


        for (var i = 0; i < group.Count; i++)
        {
            var id = group[i];
            var op = data.Ops[id];

            if (op.Op.Type.Operator != AccessorOp.Operator)
                continue;

            SequenceWrapper? left = null;
            SequenceWrapper? right = null;

            for (var l = i - 1; l >= 0; l--)
            {
                var lId = group[l];
                left = data.Ops[lId];

                if (left.IsBranch())
                    continue;

                if (!left.IsPrefix())
                    left = null;

                break;
            }

            for (var r = i + 1; r < group.Count; r++)
            {
                var rId = group[r];
                right = data.Ops[rId];

                if (!right.IsBranch())
                    continue;

                if (!right.IsPostfix())
                    right = null;

                break;
            }


            if (left == null && right == null)
            {
                op.Op.Type = DefaultOp;
                continue;
            }
            removes.Add(i);

            if ((left?.Rank ?? int.MinValue) > (right?.Rank ?? int.MinValue))
            {

            }
            else
            {

            }
        }
        foreach (var i in removes)
            ops.RemoveAt(i);
    }


    private void SequenceInner(ref List<SequenceWrapper> ops)
    {
        var groupMap = new Dictionary<int, List<int>> { { -1, [] } };
        var groupsInv = new Dictionary<int, SequenceIndex>();
        var stack = new List<int> { -1 };
        var opDict = ops.ToDictionary(x => x.Id, x => x);

        foreach (var op in ops)
        {
            var groupId = stack[stack.Count - 1];

            if (groupId >= 0 && op.Op.Type.Operator == opDict[groupId].Op.Type.GroupOperator.ToString())
            {
                stack.RemoveAt(stack.Count - 1);
                continue;
            }

            var group = groupMap[groupId];
            groupsInv[op.Id] = new SequenceIndex(groupId, group.Count);
            group.Add(op.Id);
        
            if (op.Op.Type.Category.Has(OpCategory.Group))
            {
                stack.Add(op.Id);
                groupMap.Add(op.Id, []);
            }
        }



        var ordered = ops.Where(x => !x.Op.Type.Category.Has(OpCategory.UnGroup)).ToList();
        ordered.OrderByDescending(x => x.Op.Type.Rank).ThenBy(x => x.Id);
        foreach (var op in ordered)
        {
            var inv = groupsInv[op.Id];
            var group = groupMap[inv.ParentId];

       

            if (op.IsPrefix())
            {
                for (var i = inv.Index - 1; i >= 0; i--)
                {
                    var targetIndex = group[i];
                    var left = opDict[targetIndex];
                    if (left.Op.Type.Category.Has(OpCategory.Branching))
                        continue;

                    op.AddInput(targetIndex);
                }
            }






            AddLeftInput(ops, groupMap, groupsInv, op);
        }


        foreach (var g in groupMap)
        {
            if (g.Value.Count == 0)
                continue;





            for (int i = 0; i < g.Value.Count; i++)
            {
                var op = opDict[g.Value[i]];

                if (op.IsPrefix())
                {

                }

                if (op.IsPostfix())
                {

                }



            }
        }



    }

    private static void AddLeftInput(List<SequenceWrapper> ops, Dictionary<int, List<int>> inputMap, Dictionary<int, SequenceIndex> sequenceIndex, SequenceWrapper op)
    {
        if (!op.Op.Type.Category.Has(OpCategory.Prefix))
            return;

        var index = sequenceIndex[op.Id];

        if (index.Index == 0)
            return;


        var arr = inputMap[index.ParentId];


    }








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
        ops = ops.Where(x => !x.Op.Type.Category.Has(OpCategory.UnGroup)).ToList();
    }


    private static void SequenceAffixes2(ref List<SequenceWrapper> ops)
    {
        for (int i = 0; i < ops.Count; i++)
        {
            ops[i].Id = i;
        }

        var ordered = ops.Select(x => x).ToList();
        ordered.OrderByDescending(x => x.Op.Type.Rank).ThenBy(x => x.Id);
        foreach (var op in ordered)
        {

            if (op.Op.Type.Category.Has(OpCategory.Prefix))
                op.AddLeftInput(op.LeftIndex, ops);

            if (op.Op.Type.Category.Has(OpCategory.Postfix))
                op.AddRightInput(op.RightIndex, ops);

        }
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

            seq.Id = i;
            seq.LeftIndex = entry.LeftIndex;
            seq.RightIndex = -2;

            if (seq.Op.Type.Category.Has(OpCategory.Branching))
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
        ordered.OrderByDescending(x => x.Op.Type.Rank).ThenBy(x => x.Id);
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

    */




}


/*




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
if (!Op.Type.Category.Has(OpCategory.Prefix))
return;

if (Id == 0)
{
Input.Add(null);
return;
}

var target = opsTemp[Id - 1];



targetIndex = Id - 1;







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
if (seq.LeftIndex >= 0 && seq.LeftIndex != Id)
opsTemp[seq.LeftIndex].RightIndex = Id;

if (seq.RightIndex >= 0 && seq.RightIndex != Id)
opsTemp[seq.RightIndex].LeftIndex = Id;


if (!Op.Type.Category.Has(OpCategory.Branching))
{
seq.LeftIndex = -1;
seq.RightIndex = -1;
if (Input.Count > 0 && Input[Input.Count - 1] != null)
seq.LeftIndex = Input[Input.Count - 1].Id;
}

Input.Add(seq);
}


*/