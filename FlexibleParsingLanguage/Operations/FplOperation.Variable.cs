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
        Sequence = SequenceSetVariable
    };

    internal static readonly OpConfig AccessVariable = new OpConfig("@", OpSequenceType.Virtual | OpSequenceType.Named)
    {
        Sequence = SequenceAccessVariable
    };

    private static void SequenceSetVariable(SequenceProccessData data, RawOp op)
    {
        if (op.Name == string.Empty)
            throw new QueryException(op, "SetVariable name cannot be empty");

        var input = op.LeftInput[0];
        op.ReadId = data.ActiveReadId;
        op.WriteId = data.ActiveWriteId;
        data.OpReferences[op.Name] = input;
    }

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



    private static void SequenceAccessVariable(SequenceProccessData data, RawOp op)
    {
        var name = string.IsNullOrWhiteSpace(op.Name) ? op.FallbackAccessor : op.Name;

        RawOp seek = null;
        if (string.IsNullOrEmpty(name))
            seek = data.Ops[data.RootOperatorId];
        else if (!data.OpReferences.TryGetValue(name, out seek))
            throw new QueryException(op, $"No variable named '{op.Name}'");

        data.ActiveReadId = seek.ReadId;
        data.ActiveWriteId = seek.WriteId;
        op.LeftInput.Add(seek);
    }
}
