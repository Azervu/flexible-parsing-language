using System;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;
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
    internal int LoadedWriteId { get; set; }
    internal int LoadedReadId { get; set; }
}
internal class ParseContext
{
    internal List<AccessorData> Accessors = new List<AccessorData>();

    internal char LastWriteOp { get; set; } = '*';

    internal int ReadId { get; set; }
    internal int WriteId { get; set; }
    internal WriteMode WriteMode { get; set; } = WriteMode.Read;
    internal int TokenIndex { get; set; } = 0;
    internal int LastOperatorIndex { get; set; } = 0;

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


    public const int WRITE_ROOT = 2;

    public const int READ_ROOT = 1;

    internal Tokenizer Tokenizer { get; private set; }

    public Lexicalizer()
    {
        Tokenizer = new Tokenizer("${}:", '.', "'\"", '\\');
    }

    public Parser Lexicalize(string raw)
    {
        var tokens = Tokenizer.Tokenize(raw);
        //var ops = ProcessTokens(tokens);
        var ops = ProcessTokensGroup(tokens);
        return new Parser(ops);
    }



    private ParseContext GroupContexts(List<(char, string?)> tokens)
    {
        var root = new ParseContext { Parent = null };
        var l = new List<ParseContext> { root };

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
                    break;
                case '}':
                    l.RemoveAt(l.Count - 1);
                    break;
                case ':':
                    ctx.WriteMode = WriteMode.Write;
                    ctx.Accessors.Add(new AccessorData(t, a));
                    break;
                default:

                    if (ctx.WriteMode == WriteMode.Write)
                        ctx.LastWriteOp = t;
                    ctx.LastOperatorIndex = i;
                    ctx.Accessors.Add(new AccessorData(t, a));
                    break;
            }
        }
        return root;
    }


    private List<ParseOperation> ProcessTokensGroup(List<(char, string?)> tokens)
    {
        var root = GroupContexts(tokens);
        root.ReadId = READ_ROOT;
        root.WriteId = WRITE_ROOT;

        var parseData = new ParseData
        {
            LoadedReadId = READ_ROOT,
            LoadedWriteId = -2,
            IdCounter = 3,
            Ops = new List<ParseOperation>(),
            OpsMap = new Dictionary<OperatorKey, int> {
                { new OperatorKey(-1, ROOT, null, false), 1 }
            }
        };
        ProcessContext(parseData, root);
        var debug = parseData.Ops.Select(x => $"{x.OpType} {x.IntAcc} {x.StringAcc} ").Join("\n");
        return parseData.Ops;
    }

    private void ProcessContext(ParseData data, ParseContext ctx)
    {
        var processedEnd = false;
        for (var i = 0; i < ctx.Accessors.Count; i++)
        {
            var a = ctx.Accessors[i];
            if (a.Ctx != null)
            {
                ProcessContext(data, a.Ctx);
                continue;
            }
            switch (a.Operator)
            {
                case ':':
                    ctx.WriteMode = WriteMode.Write;
                    break;
                case '.':
                case '\'':
                case '"':
                case '[':
                    if (ctx.WriteMode == WriteMode.Read)
                    {
                        ProcessReadOperator(data, ctx, a);
                    }
                    else if (ctx.LastOperatorIndex == ctx.TokenIndex)
                    {
                        processedEnd = true;
                        ProcessContextEndingOperator(data, ctx, a);
                    }
                    else
                    {
                        ProcessWriteOperator(data, ctx, a);
                    }
                    break;
                default:
                    break;
            }
        }

        if (!processedEnd && ctx.WriteMode == WriteMode.Read)
            ProcessContextEndingOperator(data, ctx, null);
    }

    private void ProcessContextEndingOperator(ParseData data, ParseContext ctx, AccessorData? acc)
    {
        EnsureReadOpLoaded(data, ctx);
        EnsureWriteOpLoaded(data, ctx, acc);


        if (acc == null || ctx.WriteMode == WriteMode.Read)
            new ParseOperation(ParseOperationType.AddFromRead);
        else if (acc.Numeric)
            new ParseOperation(ParseOperationType.WriteFromRead); //Int
        else
            new ParseOperation(ParseOperationType.WriteFromRead, acc.Accessor);
    }


    private void EnsureReadOpLoaded(ParseData data, ParseContext ctx)
    {
        if (ctx.ReadId == data.LoadedReadId)
            return;

        if (ctx.ReadId == READ_ROOT)
        {
            data.Ops.Add(new ParseOperation(ParseOperationType.ReadRoot));
            return;
        }

        if (data.SaveOps.Contains(ctx.ReadId))
        {
            data.LoadedOps.Add(ctx.ReadId);
            data.Ops.Add(new ParseOperation(ParseOperationType.ReadLoad, ctx.ReadId));
            ctx.ReadId = data.LoadedReadId;
            return;
        }

        throw new Exception("Unknown read id " + ctx.ReadId);
    }

    private void EnsureWriteOpLoaded(ParseData data, ParseContext ctx, AccessorData? acc)
    {
        if (ctx.WriteId == data.LoadedWriteId)
            return;

        if (data.SaveOps.Contains(ctx.WriteId))
        {
            data.LoadedOps.Add(ctx.WriteId);
            var o = ParseOperationType.WriteLoad;
            data.Ops.Add(new ParseOperation(o, ctx.WriteId));
            ctx.WriteId = data.LoadedWriteId;
            return;
        }

        if (ctx.WriteId != WRITE_ROOT)
            throw new Exception("Unknown write id " + ctx.WriteId);

        var key = new OperatorKey(-1, ROOT, null, true);

        if (data.OpsMap.ContainsKey(key))
        {
            data.Ops.Add(new ParseOperation(ParseOperationType.WriteRoot));
            ctx.WriteId = data.LoadedWriteId;
            return;
        }

        if (acc == null || acc.Numeric || ctx.WriteMode == WriteMode.Read)
            data.Ops.Add(new ParseOperation(ParseOperationType.WriteInitRootArray));
        else
            data.Ops.Add(new ParseOperation(ParseOperationType.WriteInitRootMap));
        ctx.WriteId = data.LoadedWriteId;
    }
}