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

internal partial class OldCompiler
{

    public const int ROOT_ID = 1;

    internal Util.Compiler Lexicalizer { get; private set; }


    public FplQuery Compile(string raw, ParsingMetaContext configContext)
    {
        var rawOps = Lexicalizer.Lexicalize(raw);


        var rootToken = new RawOp
        {
            Type = Lexicalizer.Ops[0],
            Input = rawOps
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

        return new FplQuery(ops, configContext, config);
    }


}