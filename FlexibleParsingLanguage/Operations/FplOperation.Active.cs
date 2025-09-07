using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace FlexibleParsingLanguage.Operations;

internal static partial class FplOperation
{
    internal static readonly OpConfig SetActive = new OpConfig("@", OpSequenceType.Virtual)
    {
        Sequence = SequenceSetActive,
        Compile = CompileSetActive
    };

    private static void SequenceSetActive(SequenceProccessData d, RawOp o)
    {
        d.ActiveWriteId = o.Id;
    }

    private static IEnumerable<ParseOperation> CompileSetActive(ParseData parser, RawOp op)
    {
        if (op.Input.Count != 1)
            throw new QueryException(op, "wrong number of params");

        foreach (var x in CompileLoad(parser, op))
            yield return x;

        var id = op.GetStatusId(parser);

        parser.ActiveReadId = id;
        parser.ActiveWriteId = id;
        op.ReadId = id;

        if (parser.ProccessedMetaData.TryGetValue(id, out var m) && (m.Type.CompileType & OpCompileType.WriteObject) > 0)
        {
            var accessor = m.Input[1].Accessor;
            yield return new ParseOperation(op, WriteFromRead, accessor);
        }
        else
        {
            yield return new ParseOperation(op, WriteAddRead);
        }

    }
}