using FlexibleParsingLanguage.Parse;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace FlexibleParsingLanguage.Compiler;

internal class ParseData
{
    internal List<(int, ParseOperation)> Ops { get; set; }
    internal Dictionary<(int LastOp, ParseOperation[]), int> OpsMap { get; set; }
    internal HashSet<int> SaveOps { get; set; } = new HashSet<int>();
    internal int IdCounter { get; set; }
    internal int LoadedId { get; set; }
    internal int ActiveId { get; set; }
}

internal partial class Compiler
{

    public const int ROOT_ID = 1;

    internal Lexicalizer Tokenizer { get; private set; }

    public Compiler()
    {
        Tokenizer = new Lexicalizer(
            [
                new OpTokenConfig(".", OpTokenType.Singleton),
                new OpTokenConfig("$", OpTokenType.Singleton),
                new OpTokenConfig("~", OpTokenType.Singleton),
                new OpTokenConfig("*", OpTokenType.Singleton),

                new OpTokenConfig("{", OpTokenType.Group, '}'),
                new OpTokenConfig("}", OpTokenType.Prefix),

                new OpTokenConfig("(", OpTokenType.Group, ')'),
                new OpTokenConfig(")", OpTokenType.Prefix),

                new OpTokenConfig(":", OpTokenType.Prefix),
                new OpTokenConfig("|", OpTokenType.Prefix),
                new OpTokenConfig("#", OpTokenType.Prefix),
                new OpTokenConfig("€", OpTokenType.Prefix),
                new OpTokenConfig("€€", OpTokenType.Prefix),

                new OpTokenConfig("\"", OpTokenType.Escape, '"'),
                new OpTokenConfig("'", OpTokenType.Escape, '\''),
            ],
            ".", '\\'
            );
    }

    public Parser Compile(string raw, ParsingMetaContext configContext)
    {
        var rootToken = Tokenizer.Lexicalize(raw);
        var parseData = new ParseData
        {
            ActiveId = ROOT_ID,
            LoadedId = ROOT_ID,
            IdCounter = 3,
            Ops = [
                (ROOT_ID, new ParseOperation(ParseOperationType.ReadRoot)),
                (2, new ParseOperation(ParseOperationType.Save)),
            ],
            SaveOps = [ROOT_ID],
            OpsMap = new Dictionary<(int LastOp, ParseOperation[]), int> {
                {(-1, [new ParseOperation(ParseOperationType.ReadRoot)]), ROOT_ID },
            },
        };

        var rootContex = new ParseContext(rootToken);
        var rootType = rootContex.ProcessBranch(parseData);

        var (ops, rootWt) = FilterOps(parseData, rootContex);

        var config = new ParserRootConfig { RootType = rootType };

#if DEBUG
        var token = rootToken.ToString2();

        var debug = ops.Select(x => $"{x.OpType} {x.IntAcc} {x.StringAcc} ").Join("\n");
        var s = 345534;
#endif

        return new Parser(ops, configContext, config);
    }


    private (List<ParseOperation>, WriteType) FilterOps(ParseData data, ParseContext root)
    {
        var opsMap = data.OpsMap.ToDictionary(x => x.Value, x => x.Key);
        var outOps = new List<ParseOperation>();
        var saved = new Dictionary<int, ParseOperation>();
        var loaded = new HashSet<int>();

        var opsParents = data.OpsMap.GroupBy(x => x.Key.Item1).ToDictionary(
            x => x.Key,
            x => x.Select(y => y.Key.Item2.Last().OpType).ToHashSet()
        );


        foreach (var o in data.Ops.Select(x => x.Item2))
        {
            if (o.OpType == ParseOperationType.Load)
                loaded.Add(o.IntAcc);
        }


        var rootWriteType = WriteType.None;


        foreach (var (id, o) in data.Ops)
        {
            if (rootWriteType != WriteType.None)
                rootWriteType = o.OpType.GetWriteType();


            if (o.OpType == ParseOperationType.Save && !loaded.Contains(o.IntAcc))
                continue;

            if (o.OpType == ParseOperationType.WriteFlatten)
                o.IntAcc = (int)GetWriteType(opsParents[id]);

            outOps.Add(o);
        }

        if (rootWriteType == WriteType.None)
            rootWriteType = WriteType.Array;


        return (outOps, rootWriteType);
    }

    private WriteType GetWriteType(HashSet<ParseOperationType> opsTypes)
    {
        var ii = WriteType.None;
        foreach (var childOp in opsTypes)
        {
            var wt = childOp.GetWriteType();
            if (wt > WriteType.None)
                ii = wt;
        }

        return ii;
    }
}