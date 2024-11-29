namespace FlexibleParsingLanguage;

public struct OperatorKey
{
    public char Operator { get; internal set; }
    public int TargetId { get; internal set; }
    public int AccessorId { get; internal set; }
    public string? Accessor { get; internal set; }
    public bool Write { get; internal set; }

    public OperatorKey(int targetId, char op, string acc, bool write = false)
    {
        TargetId = targetId;
        Operator = op;
        Accessor = acc;
        AccessorId = -1;
        Write = write;
    }

    public OperatorKey(int targetId, char op, int accessorId, bool write = false)
    {
        TargetId = targetId;
        Operator = op;
        AccessorId = accessorId;
        Write = write;
    }
}

internal enum WriteMode
{
    Read,
    Write,
    Written,
}

internal class ParseData
{
    internal List<ParseOperation> Ops { get; set; }
    internal Dictionary<OperatorKey, int> OpsMap { get; set; }

    internal HashSet<int> SaveOps { get; set; } = new HashSet<int>();
    internal HashSet<int> LoadedOps { get; set; } = new HashSet<int>();

    internal int IdCounter { get; set; }
    internal int LoadedId { get; set; }
}
internal class ParseContext
{
    internal List<AccessorData> Accessors = new List<AccessorData>();
    internal int ActiveId { get; set; }
    internal WriteMode WriteMode { get; set; } = WriteMode.Read;
    internal ParseContext Parent { get; set; }
}

internal class AccessorData
{
    internal char Operator { get; set; }
    internal string? Accessor { get; set; }
    internal ParseContext Ctx { get; set; }

    internal bool Numeric { get => Operator == '['; }

    internal AccessorData(char op, string? acc, ParseContext ctx = null)
    {
        Operator = op;
        Accessor = acc;
        Ctx = ctx;
    }

}

internal partial class Lexicalizer
{
    public const char ROOT = '$';

    public const char BRANCH = '{';

    public const char UNBRANCH = '}';

    public const char SEPARATOR = ':';

    public const char ACCESS = '.';

    public const char WRITE = '_';


    public const int ROOT_ID = 1;

    internal Tokenizer Tokenizer { get; private set; }

    public Lexicalizer()
    {
        Tokenizer = new Tokenizer("", "${}:*", '.', "'\"", '\\');
    }

    public Parser Lexicalize(string raw)
    {
        var tokens = Tokenizer.Tokenize(raw);
        var root = GroupContexts(tokens);
        var (ops, config) = ProcessTokensGroup(root);


        var debug = ops.Select(x => $"{x.OpType} {x.IntAcc} {x.StringAcc} ").Join("\n");

        return new Parser(ops, config);
    }



    private ParseContext GroupContexts(List<(char, string?)> tokens)
    {
        var root = new ParseContext { Parent = null };
        var l = new List<ParseContext> { root };

        var startI = 0;

        for (var i = 0; i < tokens.Count; i++)
        {
            var (t, a) = tokens[i];

            var ctx = l.Last();
            switch (t)
            {
                case '{':
                    var n = new ParseContext { Parent = ctx, WriteMode = WriteMode.Read };
                    ctx.Accessors.Add(new AccessorData(t, a, n));
                    l.Add(n);
                    startI = i + 1;
                    break;
                case '}':
                    l.RemoveAt(l.Count - 1);
                    break;
                case ':':
                    ctx.WriteMode = WriteMode.Write;
                    ctx.Accessors.Add(new AccessorData(t, a));
                    break;
                default:
                    ctx.Accessors.Add(new AccessorData(t, a));
                    break;
            }
        }
        return root;
    }

    private (List<ParseOperation>, ParserConfig) ProcessTokensGroup(ParseContext root)
    {
        var config = new ParserConfig();

        root.ActiveId = ROOT_ID;

        var parseData = new ParseData
        {
            LoadedId = ROOT_ID,
            IdCounter = 3,
            Ops = new List<ParseOperation>(),
            OpsMap = new Dictionary<OperatorKey, int> {
                { new OperatorKey(-1, ROOT, null, false), 1 }
            }
        };
        ProcessContext(config, parseData, root, null);

        var outOps = new List<ParseOperation>();

        foreach (var o in parseData.Ops)
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

    private void ProcessContext(ParserConfig config, ParseData data, ParseContext ctx, ParseContext parent)
    {
        if (parent != null)
        {
            ctx.ActiveId = parent.ActiveId;
            ctx.ActiveId = parent.ActiveId;
        }


        ctx.WriteMode = WriteMode.Read;
        var processedEnd = false;
        for (var i = 0; i < ctx.Accessors.Count; i++)
        {
            var a = ctx.Accessors[i];
            if (a.Ctx != null)
            {
                ProcessContext(config, data, a.Ctx, ctx);
                continue;
            }
            switch (a.Operator)
            {
                case ':':
                    ctx.WriteMode = WriteMode.Write;
                    break;
                case '*':
                    if (ctx.WriteMode == WriteMode.Read)
                        data.Ops.Add(new ParseOperation(ParseOperationType.ReadForeach));
                    else
                        data.Ops.Add(new ParseOperation(ParseOperationType.WriteForeachArray));
                    break;
                case '.':
                case '\'':
                case '"':
                case '[':
                    if (ctx.WriteMode == WriteMode.Read)
                    {
                        ProcessReadOperator(data, ctx, a);
                    }
                    else if (i == ctx.Accessors.Count - 1)
                    {
                        processedEnd = true;
                        ProcessContextEndingOperator(config, data, ctx, a);
                    }
                    else
                    {
                        ProcessWriteOperator(config, data, ctx, a);
                    }
                    break;
                default:
                    break;
            }
        }

        if (!processedEnd && ctx.WriteMode == WriteMode.Read)
            ProcessContextEndingOperator(config, data, ctx, null);
    }

    private void ProcessContextEndingOperator(ParserConfig config, ParseData data, ParseContext ctx, AccessorData? acc)
    {
        if (ctx.Accessors.Count != 0 && ctx.Accessors.Last().Ctx != null)
            return;

        EnsureReadOpLoaded(data, ctx);
        EnsureWriteOpLoaded(config, data, ctx, acc);

        if (acc == null || ctx.WriteMode == WriteMode.Read)
            data.Ops.Add(new ParseOperation(ParseOperationType.AddFromRead));
        else if (acc.Numeric)
            data.Ops.Add(new ParseOperation(ParseOperationType.WriteFromRead)); //Int
        else
            data.Ops.Add(new ParseOperation(ParseOperationType.WriteFromRead, acc.Accessor));
    }


    private void EnsureReadOpLoaded(ParseData data, ParseContext ctx)
    {
        if (ctx.ActiveId == data.LoadedId)
            return;

        if (ctx.ActiveId == ROOT_ID)
        {
            data.Ops.Add(new ParseOperation(ParseOperationType.Root));
            return;
        }

        if (data.SaveOps.Contains(ctx.ActiveId))
        {
            data.LoadedOps.Add(ctx.ActiveId);
            data.Ops.Add(new ParseOperation(ParseOperationType.Load, ctx.ActiveId));
            data.LoadedId = ctx.ActiveId;
            return;
        }

        throw new Exception("Unknown read id " + ctx.ActiveId);
    }

    private void EnsureWriteOpLoaded(ParserConfig config, ParseData data, ParseContext ctx, AccessorData? acc)
    {
        if (ctx.ActiveId == data.LoadedId)
            return;

        if (data.SaveOps.Contains(ctx.ActiveId))
        {
            data.LoadedOps.Add(ctx.ActiveId);
            data.Ops.Add(new ParseOperation(ParseOperationType.Load, ctx.ActiveId));
            ctx.ActiveId = data.LoadedId;
            return;
        }

        if (ctx.ActiveId != ROOT_ID)
            throw new Exception("Unknown write id " + ctx.ActiveId);

        var key = new OperatorKey(-1, ROOT, null, true);

        if (data.OpsMap.ContainsKey(key))
        {
            data.Ops.Add(new ParseOperation(ParseOperationType.Root));
            ctx.ActiveId = data.LoadedId;
            return;
        }
        data.OpsMap.Add(key, ROOT_ID);

        config.WriteArrayRoot = acc == null || acc.Numeric || ctx.WriteMode == WriteMode.Read;
        ctx.ActiveId = data.LoadedId;
    }
}