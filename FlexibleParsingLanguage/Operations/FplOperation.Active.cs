using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace FlexibleParsingLanguage.Operations;

internal static partial class FplOperation
{
    internal static readonly OpConfig SetActive = new OpConfig("@", OpSequenceType.Virtual, CompileSetActive);

    private static IEnumerable<ParseOperation> CompileSetActive(ParseData parser, RawOp op)
    {
        /*

        RawOp? ctx = null;
        var (ancestorId, i) = data.GroupParents[op.Id];
            var ancestor = data.Ops[ancestorId];

            if (ancestor.LeftInput.Count >= 0)
                op.LeftInput.Add(ancestor.LeftInput[0]);
            */


        /*
         * 
         * 

private void AddParentInput(SequenceProccessData data, RawOp op)
{
    RawOp? ctx = null;
    var (ancestorId, i) = data.GroupParents[op.Id];
    var ancestor = data.Ops[ancestorId];

    if (ancestor.LeftInput.Count >= 0)
        op.LeftInput.Add(ancestor.LeftInput[0]);
}


        */



        if (op.Input.Count != 1)
            throw new QueryException(op, "wrong number of params");

        foreach (var x in EnsureLoaded(parser, op))
            yield return x;

        var id = op.GetStatusId(parser);


        if (parser.ProccessedMetaData.TryGetValue(id, out var m) && (m.Type.CompileType & OpCompileType.WriteObject) > 0)
        {
            var accessor = m.Input[1].Accessor;
            yield return new ParseOperation(op, ParsesOperationType.WriteFromRead, accessor);
        }
        else
        {
            yield return new ParseOperation(op, ParsingContext.WriteAddRead);

        }
    }
}