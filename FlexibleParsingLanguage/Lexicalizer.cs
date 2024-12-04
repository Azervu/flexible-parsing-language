namespace FlexibleParsingLanguage;

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

    internal bool Numeric { get => Operator == '[' || Operator == '*'; }

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
        Tokenizer = new Tokenizer("", "${}:*~", '.', "'\"", '\\');
    }

    public Parser Lexicalize(string raw)
    {
        var tokens = Tokenizer.Tokenize(raw);
        var root = GroupContexts(tokens);
        var (ops, config) = ProcessTokensGroup(root);


#if DEBUG

        var debug = ops.Select(x => $"{x.OpType} {x.IntAcc} {x.StringAcc} ").Join("\n");

        var s = 345534;
#endif



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






        var config = new ParserConfig
        {
            WriteArrayRoot = FirstRead(root)?.Numeric ?? true
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

        /*
        foreach (var w in parseData.Ops)
        {
            if (!w.Item2.OpType.IsWriteOperation())
                continue;

            var operatorId = w.Item1;

            while (opsMap.ContainsKey(operatorId))
            {
                var (parentId, op) = opsMap[operatorId];

                if (!op.OpType.IsReadOperation())
                {
                    operatorId = parentId;
                    continue;
                }
                config.WriteArrayRoot = op.OpType.IsNumericOperation();
                break;
            }
            break;
        }
        */


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
        var processedEnd = false;
        for (var i = 0; i < ctx.Accessors.Count; i++)
        {
            var accessor = ctx.Accessors[i];
            if (accessor.Ctx != null)
            {
                ProcessContext(config, parser, accessor.Ctx, ctx);
                continue;
            }

            ParseOperation? op = null;

            switch (accessor.Operator)
            {
                case '$':

                    if (ctx.WriteMode == WriteMode.Read)
                        op = new ParseOperation(ParseOperationType.ReadRoot);
                    else
                        op = new ParseOperation(ParseOperationType.WriteRoot);

                    break;
                case ':':
                    ctx.WriteMode = WriteMode.Write;
                    break;
                case '*':

                    if (ctx.WriteMode == WriteMode.Read)
                    {
                        op = new ParseOperation(ParseOperationType.ReadFlatten, accessor.Accessor);
                    }
                    else
                    {
                        var nextNumeric = NextReadOperator(i, ctx)?.Numeric ?? true;
                        op = new ParseOperation(nextNumeric ? ParseOperationType.WriteFlattenArray : ParseOperationType.WriteFlattenObj);
                    }
                    break;
                case '~':
                    if (ctx.WriteMode == WriteMode.Read)
                        op = new ParseOperation(ParseOperationType.ReadName);
                    else
                        op = new ParseOperation(ParseOperationType.WriteNameFromRead);
                    break;
                case '.':
                case '\'':
                case '"':
                case '[':
                    if (ctx.WriteMode == WriteMode.Read)
                    {
                        op = new ParseOperation(ParseOperationType.Read, accessor.Accessor);
                    }
                    else if (i == ctx.Accessors.Count - 1)
                    {
                        processedEnd = true;
                        op = ProcessContextEndingOperator(config, parser, ctx, accessor);
                    }
                    else
                    {
                        op = ProcessWriteOperator(i, config, parser, ctx, accessor);
                    }
                    break;
                default:
                    break;
            }

            HandleOp(config, parser, ctx, op);
        }



        if (!processedEnd && ctx.WriteMode == WriteMode.Read)
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



    private ParseOperation? ProcessContextEndingOperator(ParserConfig config, ParseData data, ParseContext ctx, AccessorData? acc)
    {

        if (ctx.Accessors.Count != 0 && ctx.Accessors.Last().Ctx != null)
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

    private void EnsureWriteOpLoaded(ParserConfig config, ParseData data, ParseContext ctx, AccessorData? acc)
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