using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FlexibleParsingLanguage.Compiler;

public partial class FplCompiler
{
    private void RemapGroupInputHierarchy(SequenceProccessData data, ref List<RawOp> ops)
    {
        var proccessed = new Dictionary<int, RawOp?>();
        foreach (var op in ops.Where(x => x.Type.SequenceType.All(OpSequenceType.Group)))
        {
            if (proccessed.ContainsKey(op.Id))
                continue;

#if DEBUG
            var root = new Dictionary<int, int>();
            foreach (var o in data.Ops)
            {

            }
#endif

            var (target, remaps) = FindRemapTarget(data, op, proccessed);
            foreach (var r in remaps)
            {
                r.LeftInput.Clear();
                if (target != null)
                    r.LeftInput.Add(target);
                proccessed[r.Id] = target;
            }
        }

    }


    private (RawOp?, List<RawOp>) FindRemapTarget(SequenceProccessData data, RawOp op, Dictionary<int, RawOp?> proccessed)
    {
        var active = op;
        var remaps = new List<RawOp> { op };

#if DEBUG
        var debugPath = op.Id.ToString();
        var loopCheck = new HashSet<int>();
#endif



        while (true)
        {

#if DEBUG
            if (loopCheck.Contains(active.Id))
                throw new QueryException(active, "grouping loop", true);
            loopCheck.Add(active.Id);
#endif

            if (proccessed.ContainsKey(active.Id))
            {
                return (active.LeftInput.Count > 0 ? active.LeftInput[0] : null, remaps);
            }
            else if (active.LeftInput.Count > 0)
            {
                active = active.LeftInput[0];

#if DEBUG
                debugPath += $" -l'{active.Type.Operator}'> {active.Id}";
#endif

                if (!active.Type.SequenceType.All(OpSequenceType.Group))
                    return (active, remaps);
            }
            else if (data.AffixParents.TryGetValue(active.Id, out var x))
            {
                active = data.Ops[x.ParentId];
#if DEBUG
                debugPath += $" -p'{active.Type.Operator}'> {active.Id}";
#endif
            }
            else if (active.Id == RootGroupId)
            {
                return (active, remaps);
            }
            else if (op.Id == active.Id)
            {
                throw new QueryException(active, "grouping loop", true);
            }
            remaps.Add(active);
        }
    } 



}