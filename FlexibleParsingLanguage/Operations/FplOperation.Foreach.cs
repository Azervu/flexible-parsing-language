using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Operations;

internal partial class FplOperation
{
    internal static readonly OpConfig Foreach = new OpConfig("*", OpSequenceType.LeftInput)
    {
        CompileType = OpCompileType.ReadArray,
        Sequence = SequenceForeach,
        Compile = CompileForeach,

    };

    private static void SequenceForeach(SequenceProccessData d, RawOp o)
    {
        d.ActiveReadId = o.Id;
    }

    private static IEnumerable<ParseOperation> CompileForeach(ParseData p, RawOp o)
    {
        foreach (var x in CompileSaveUtil(p, o, 1, [new ParseOperation(o, OperationForeach)]))
            yield return x;
        //yield return new ParseOperation(o, OperationForeach, new ParseOperationData { StringAcc = null, IntAcc = o.Input[0].ReadId, Raw = 1 });

        p.ActiveReadId = o.Id;
        o.ReadId = o.Id;
    }



    internal static void OperationForeach(FplQuery parser, ParsingContext context, ParseOperationData op)
    {
        var opId = op.Id;

        var focus = context.Focus;

        var result = new List<FocusEntry>();
        //var active = focus.Reads[op.IntAcc];
        var active = focus.Reads[focus.Active.ReadId];
        foreach (var r in active)
        {
            var m = context.GetReadingModule(r.Value);
            foreach (var kv in m.Foreach(r.Value.V))
            {
                focus.SequenceIdCounter++;
                result.Add(new FocusEntry
                {
                    Key = new ValueWrapper(kv.Key),
                    Value = new ValueWrapper(kv.Value),
                    SequenceId = focus.SequenceIdCounter
                });
                focus.Sequences[r.SequenceId].ChildrenIds.Add(focus.SequenceIdCounter);
                focus.Sequences[focus.SequenceIdCounter] = new ParsingSequence { ParentId = r.SequenceId };
            }
        }
        focus.Reads[opId] = result;
        focus.Active = new ParsingNode(opId, focus.Active.WriteId, focus.Active.ConfigId);
    }

}