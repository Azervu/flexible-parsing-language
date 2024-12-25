using FlexibleParsingLanguage.Compiler.Util;
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

    internal Lexicalizer Lexicalizer { get; private set; }

    public Compiler()
    {
        Lexicalizer = new Lexicalizer([
            new OpConfig("{", OpCategory.Root | OpCategory.Group | OpCategory.Branching | OpCategory.LeftInput, 100, "}"),
            new OpConfig(".", OpCategory.RightInput | OpCategory.LeftInput | OpCategory.Default),
            new OpConfig("(", OpCategory.Group | OpCategory.Virtual | OpCategory.Accessor, 100, ")"),
            new OpConfig("$", OpCategory.Param),
            new OpConfig("~", OpCategory.LeftInput),
            new OpConfig("*", OpCategory.LeftInput),
            new OpConfig(":", OpCategory.RightInput | OpCategory.LeftInput),
            new OpConfig("|", OpCategory.RightInput | OpCategory.LeftInput),
            new OpConfig("@", OpCategory.ParentInput | OpCategory.Virtual),
            new OpConfig("#", OpCategory.RightInput | OpCategory.LeftInput),
            new OpConfig("##", OpCategory.RightInput | OpCategory.LeftInput),
            new OpConfig("\"", OpCategory.Literal, -1, "\""),
            new OpConfig("'", OpCategory.Literal, -1, "\'"),
            new OpConfig("\\", OpCategory.Unescape, -1)
        ]);
    }

    public Parser Compile(string raw, ParsingMetaContext configContext)
    {
        var rawOps = Lexicalizer.Lexicalize(raw);


        var rootToken = new RawOp
        {
            Type = Lexicalizer.Ops[0],
            Children = rawOps
        };

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

        var (ops, rootWt) = ParsesOperationType.CompileOperations(parseData, rootContex);

        var config = new ParserRootConfig { RootType = rootType };

#if DEBUG
        var token = rootToken.ToString();

        //var debug = rawOps.Select(x => $"{x.GetMetaData().Name} {x.IntAcc} {x.StringAcc} ").Join("\n");
        var s = 345534;
#endif

        return new Parser(ops, configContext, config);
    }


}