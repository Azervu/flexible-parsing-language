using FlexibleParsingLanguage.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Operations;

internal static partial class FplOperation
{
    internal static readonly OpConfig SetVariable = new OpConfig("@@", OpSequenceType.LeftInput | OpSequenceType.Virtual | OpSequenceType.Named | OpSequenceType.Branching)
    {
        Sequence = SetVariableAction
    };

    internal static readonly OpConfig AccessVariable = new OpConfig("@", OpSequenceType.Virtual | OpSequenceType.Named)
    {
        Sequence = AccessVariableAction
    };

    private static void SetVariableAction(SequenceProccessData data, RawOp op)
    {
        if (op.Name == string.Empty)
            throw new QueryException(op, "SetVariable name cannot be empty");

        /*
        RawOp seek = op;

        while (true)
        {
            if (!data.AffixParents.TryGetValue(seek.Id, out var x))
                throw new QueryException(op, $"Variable '{op.Name}' not found in the current context. Ensure it is defined before use.");

            var parentChildren = data.Ops[x.ParentId].AffixChildren[x.Index];
            var index = parentChildren.IndexOf(seek.Id);

            if (index == -1)
                throw new QueryException(op, $"Index not found in ({x.ParentId}, {x.Index}) [{parentChildren.Select(x => x.ToString()).Join(", ")}]");

            if (index > 0)
                seek = data.Ops[parentChildren[index - 1]];
            else
                seek = data.Ops[x.ParentId];

            if (seek.Type.SequenceType.Any(OpSequenceType.Root) || !seek.Type.SequenceType.Any(OpSequenceType.Branching | OpSequenceType.VirtualInput))
                break;
        }
                data.OpReferences[op.Name] = seek;
        */

        data.OpReferences[op.Name] = op.LeftInput[0];
    }

    private static void AccessVariableAction(SequenceProccessData data, RawOp op)
    {
        if (op.Name != string.Empty)
        {
            if (!data.OpReferences.ContainsKey(op.Name))
                throw new QueryException(op, $"No variable named '{op.Name}'");

            op.LeftInput.Add(data.OpReferences[op.Name]);
        }
        else
        {
            RawOp? ctx = null;
            var (ancestorId, i) = data.GroupParents[op.Id];
            var ancestor = data.Ops[ancestorId];

            if (ancestor.LeftInput.Count >= 0)
                op.LeftInput.Add(ancestor.LeftInput[0]);
        }
    }
}
