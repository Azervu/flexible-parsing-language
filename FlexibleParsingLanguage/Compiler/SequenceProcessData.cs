using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler;

internal class SequenceProccessData
{
    internal Dictionary<int, RawOp> Ops { get; set; }

    internal Dictionary<string, RawOp> OpReferences { get; set; } = new Dictionary<string, RawOp>();

    internal Dictionary<int, (int ParentId, int Index)> GroupParents { get; set; } = new Dictionary<int, (int ParentId, int Index)>();

    internal Dictionary<int, (int ParentId, int Index)> AffixParents { get; set; } = new Dictionary<int, (int ParentId, int Index)>();

    internal int GetAffixIndex(RawOp op)
    {
        var (parentId, groupIndex) = AffixParents[op.Id];

        var children = Ops[parentId].AffixChildren[groupIndex];
        var i = children.IndexOf(op.Id);
        if (i >= 0)
            return i;
        throw new QueryException(op, $"Index not found in ({parentId}, {groupIndex}) [{children.Select(x => x.ToString()).Join(", ")}]");
    }
}
