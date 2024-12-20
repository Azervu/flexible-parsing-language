﻿using FlexibleParsingLanguage.Parse;
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
                new OpTokenConfig("@", OpTokenType.Prefix),
                new OpTokenConfig("#", OpTokenType.Prefix),
                new OpTokenConfig("##", OpTokenType.Prefix),
                //new OpTokenConfig("€", OpTokenType.Prefix),
                //new OpTokenConfig("€€", OpTokenType.Prefix),

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
                (ROOT_ID, new ParseOperation(ParsesOperationType.ReadRoot)),
                (2, new ParseOperation(ParsesOperationType.Save)),
            ],
            SaveOps = [ROOT_ID],
            OpsMap = new Dictionary<(int LastOp, ParseOperation[]), int> {
                {(-1, [new ParseOperation(ParsesOperationType.ReadRoot)]), ROOT_ID },
            },
        };

        var rootContex = new ParseContext(rootToken);
        var rootType = rootContex.ProcessBranch(parseData);

        var (ops, rootWt) = ProcessOps(parseData, rootContex);

        var config = new ParserRootConfig { RootType = rootType };

#if DEBUG
        var token = rootToken.ToString2();

        var debug = ops.Select(x => $"{x.OpType.GetMetaData().Name} {x.IntAcc} {x.StringAcc} ").Join("\n");
        var s = 345534;
#endif

        return new Parser(ops, configContext, config);
    }


}