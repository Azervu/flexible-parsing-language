using FlexibleParsingLanguage.Parse;
namespace FlexibleParsingLanguage.Compiler;

internal enum WriteMode
{
    Read,
    Write,
    Written,
}

internal class ParseData
{
    internal List<(int, ParseOperation)> Ops { get; set; }
    internal Dictionary<(int LastOp, ParseOperation), int> OpsMap { get; set; }

    internal HashSet<int> SaveOps { get; set; } = new HashSet<int>();
    internal HashSet<int> LoadedOps { get; set; } = new HashSet<int>();

    internal int IdCounter { get; set; }
    internal int LoadedId { get; set; }
}

internal partial class Compiler
{
    public const char ROOT = '$';

    public const char BRANCH = '{';

    public const char UNBRANCH = '}';

    public const char SEPARATOR = ':';

    public const char ACCESS = '.';

    public const char WRITE = '_';

    public const int ROOT_ID = 1;

    internal Lexicalizer Tokenizer { get; private set; }

    public Compiler()
    {
        Tokenizer = new Lexicalizer(
            [
                new OpTokenConfig(".", OpTokenType.Singleton),
                new OpTokenConfig("$", OpTokenType.Singleton),
                new OpTokenConfig("~", OpTokenType.Singleton),

                new OpTokenConfig("{", OpTokenType.Group, '}'),
                new OpTokenConfig("}", OpTokenType.Prefix),

                new OpTokenConfig("(", OpTokenType.Group, ')'),
                new OpTokenConfig(")", OpTokenType.Prefix),

                new OpTokenConfig("|", OpTokenType.Prefix),
                new OpTokenConfig("#", OpTokenType.Prefix),
                new OpTokenConfig("€", OpTokenType.Prefix),
                new OpTokenConfig("€€", OpTokenType.Prefix),
                new OpTokenConfig("*", OpTokenType.Prefix),

                new OpTokenConfig("\"", OpTokenType.Escape, '"'),
                new OpTokenConfig("'", OpTokenType.Escape, '\''),
            ],
            ".", '\\'
            );
    }

    public Parser Compile(string raw, ParsingConfigContext configContext)
    {
        var root = Tokenizer.Lexicalize(raw);

        var (ops, config) = ProcessTokensGroup(root);


#if DEBUG

        //var debug = ops.Select(x => $"{x.OpType} {x.IntAcc} {x.StringAcc} ").Join("\n");

        var s = 345534;
#endif

        return new Parser(ops, config, configContext);
    }

    private (List<ParseOperation>, ParserConfig) ProcessTokensGroup(TokenGroup rootToken)
    {
        var root = new ParseContext(rootToken);
        var config = new ParserConfig
        {
            WriteArrayRoot = root.FirstRead()?.Numeric ?? true
        };

        root.ActiveId = ROOT_ID;

        var parseData = new ParseData
        {
            LoadedId = ROOT_ID,
            IdCounter = 3,
            Ops = [],
            OpsMap = new Dictionary<(int LastOp, ParseOperation), int> { { (-1, new ParseOperation(ParseOperationType.ReadRoot)), 1 } },
        };

        ProcessContext(config, parseData, root, null);

        var opsMap = parseData.OpsMap.ToDictionary(x => x.Value, x => x.Key);

        var outOps = new List<ParseOperation>();
        foreach (var o in parseData.Ops.Select(x => x.Item2))
        {
            if (o.OpType == ParseOperationType.Save)
            {
                if (!parseData.LoadedOps.Contains(o.IntAcc))
                {
                    continue;
                }
            }
            outOps.Add(o);
        }
        return (outOps, config);
    }

    private void ProcessContext(ParserConfig config, ParseData parser, ParseContext ctx, ParseContext parent)
    {
        if (parent != null)
        {
            ctx.ActiveId = parent.ActiveId;
            ctx.ActiveId = parent.ActiveId;
        }

        ctx.WriteMode = WriteMode.Read;
        ctx.ProcessedEnd = false;

        ProcessOperation(config, parser, ctx, parent);

        if (ctx.Accessors != null)
        {
            foreach (var acc in ctx.Accessors)
            {
                ProcessContext(config, parser, acc, ctx);
            }
        }

        /*
        for (ctx.Index = 0; ctx.Index < ctx.Accessors.Count; ctx.Index++)
        {
            var accessor = ctx.Accessors[ctx.Index];
            foreach (var op in ProcessOperation(config, parser, ctx, accessor))
                HandleOp(config, parser, ctx, op);
        }
        */

        if (!ctx.ProcessedEnd && ctx.WriteMode == WriteMode.Read)
        {
            var op = ProcessContextEndingOperator(config, parser, ctx, null);
            HandleOp(config, parser, ctx, op);
        }

    }


    private void ProcessContext2(ParserConfig config, ParseData parser, ParseContext ctx, ParseContext parent)
    {
        if (parent != null)
        {
            ctx.ActiveId = parent.ActiveId;
            ctx.ActiveId = parent.ActiveId;
        }

        ctx.WriteMode = WriteMode.Read;
        ctx.ProcessedEnd = false;


        if (ctx.Accessors == null)
            return;

        for (ctx.Index = 0; ctx.Index < ctx.Accessors.Count; ctx.Index++)
        {
            var accessor = ctx.Accessors[ctx.Index];
            foreach (var op in ProcessOperation(config, parser, ctx, accessor))
                HandleOp(config, parser, ctx, op);
        }

        if (!ctx.ProcessedEnd && ctx.WriteMode == WriteMode.Read)
        {
            var op = ProcessContextEndingOperator(config, parser, ctx, null);
            HandleOp(config, parser, ctx, op);
        }

    }

    private void HandleOp(ParserConfig config, ParseData parser, ParseContext ctx, ParseOperation? op)
    {
        if (op == null)
            return;

        var activeId = op.OpType == ParseOperationType.ReadRoot ? -1 : ctx.ActiveId;
        var key = (activeId, op);
        if (parser.OpsMap.TryGetValue(key, out var readId))
        {
            ctx.ActiveId = readId;
            return;
        }

        EnsureReadOpLoaded(parser, ctx);

        ctx.ActiveId = ++parser.IdCounter;
        parser.SaveOps.Add(ctx.ActiveId);
        parser.LoadedId = ctx.ActiveId;

        if (parser.OpsMap.ContainsKey(key))
        {
            throw new Exception($"Repeated {key.activeId} ");
        }
        parser.Ops.Add((ctx.ActiveId, op));
        parser.OpsMap.Add(key, ctx.ActiveId);

        parser.IdCounter++;
        var saveOp = new ParseOperation(ParseOperationType.Save, ctx.ActiveId);
        parser.Ops.Add((parser.IdCounter, saveOp));
        parser.OpsMap.Add((activeId, saveOp), parser.IdCounter);


    }



    private ParseOperation? ProcessContextEndingOperator(ParserConfig config, ParseData data, ParseContext ctx, ParseContext? acc)
    {

        if (ctx.Accessors.Count != 0 && ctx.Accessors.Last() != null)
            return null;

        EnsureReadOpLoaded(data, ctx);
        EnsureWriteOpLoaded(config, data, ctx, acc);

        if (acc == null || ctx.WriteMode == WriteMode.Read)
            return new ParseOperation(ParseOperationType.WriteAddRead);
        else if (acc.Numeric)
            return new ParseOperation(ParseOperationType.WriteFromRead); //Int
        else
            return new ParseOperation(ParseOperationType.WriteFromRead, acc.Accessor);
    }


    private void EnsureReadOpLoaded(ParseData data, ParseContext ctx)
    {
        if (ctx.ActiveId == data.LoadedId)
            return;

        if (ctx.ActiveId == ROOT_ID)
        {
            data.Ops.Add((ROOT_ID, new ParseOperation(ParseOperationType.ReadRoot)));
            return;
        }

        if (data.SaveOps.Contains(ctx.ActiveId))
        {
            data.LoadedOps.Add(ctx.ActiveId);
            data.Ops.Add((-1, new ParseOperation(ParseOperationType.Load, ctx.ActiveId)));
            data.LoadedId = ctx.ActiveId;
            return;
        }

        throw new Exception("Query parsing error | Unknown read id " + ctx.ActiveId);
    }

    private void EnsureWriteOpLoaded(ParserConfig config, ParseData data, ParseContext ctx, ParseContext? acc)
    {

        if (config.WriteArrayRoot == null)
            config.WriteArrayRoot = acc == null || acc.Numeric || ctx.WriteMode == WriteMode.Read;

        if (ctx.ActiveId == data.LoadedId)
            return;

        if (data.SaveOps.Contains(ctx.ActiveId))
        {
            data.LoadedOps.Add(ctx.ActiveId);
            data.Ops.Add((-1, new ParseOperation(ParseOperationType.Load, ctx.ActiveId)));
            ctx.ActiveId = data.LoadedId;
            return;
        }

        if (ctx.ActiveId != ROOT_ID)
            throw new Exception("Unknown write id " + ctx.ActiveId);

        var key = (-1, new ParseOperation(ParseOperationType.ReadRoot));
        if (data.OpsMap.ContainsKey(key))
        {
            data.Ops.Add((data.LoadedId, key.Item2));
            ctx.ActiveId = data.LoadedId;
            return;
        }
        data.OpsMap.Add(key, ROOT_ID);


        ctx.ActiveId = data.LoadedId;
    }
}