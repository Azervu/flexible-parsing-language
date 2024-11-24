using System.ComponentModel;
using System.Diagnostics.Metrics;
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

internal struct AccessorData
{
    internal List<(char, string?)> Tokens { get; set; }
    internal int Index { get; set; }

    internal bool Numeric { get => Operator == '['; }
    internal char NextToken { get => Index + 1 < Tokens.Count ? Tokens[Index + 1].Item1 : ' '; }
    internal char Operator { get => Tokens[Index].Item1; }
    internal string Accessor { get => Tokens[Index].Item2; }

    internal char NextActiveChar()
    {
        var depth = 0;
        for (var i = Index + 1; i < Tokens.Count; i++)
        {
            var (token, accessor) = Tokens[i];
            switch (token)
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth < 0)
                        return ' ';
                    break;
                default:
                    if (depth == 0)
                        return token;
                    break;
            }
        }
        return ' ';
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
    internal int IdCounter { get; set; }
    internal int LoadedWriteId { get; set; }
    internal int LoadedReadId { get; set; }
}


internal class ParseContext
{
    internal List<TestAccessor> Accessors = new List<TestAccessor>();
    internal int ReadId { get; set; }
    internal int WriteId { get; set; }
    internal WriteMode WriteMode { get; set; } = WriteMode.Read;
    internal int TokenIndex { get; set; } = 0;
    internal ParseContext Parent { get; set; }
}
internal class TestAccessor
{
    internal char Op { get; set; }
    internal string? Acc { get; set; }
    internal ParseContext Ctx { get; set; }

    internal TestAccessor(char op, string? acc, ParseContext ctx = null)
    {
        Op = op;
        Acc = acc;
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
        foreach (var (t, a) in tokens)
        {
            var ctx = l.Last();
            switch (t)
            {
                case '{':
                    var n = new ParseContext { Parent = ctx };
                    ctx.Accessors.Add(new TestAccessor(t, a, n));
                    l.Add(n);
                    break;
                case '}':
                    l.RemoveAt(l.Count - 1);
                    break;
                default:
                    ctx.Accessors.Add(new TestAccessor(t, a));
                    break;
            }
        }
        return root;


    }



    private List<ParseOperation> ProcessTokensGroup(List<(char, string?)> tokens)
    {
        var root = GroupContexts(tokens);
        var active = root;

        while (true)
        {
            if (active == null)
                break;
            if (active.TokenIndex >= active.Accessors.Count)
            {
                active = active.Parent;
                continue;
            }

            var a = active.Accessors[active.TokenIndex];
            active.TokenIndex++;
            if (a.Ctx != null)
            {
                active = a.Ctx;
            }
            else
            {
                ProcessOperator(active, a);
            }

        }

        return null;

    }

    private void ProcessOperator(ParseContext ctx, TestAccessor acc)
    {
        switch (acc.Op)
        {
            case ':':
                ctx.WriteMode = WriteMode.Write;
                break;
            case '.':
            case '\'':
            case '"':
            case '[':
                break;
            default:
                break;
        }
    }





















    private List<ParseOperation> ProcessTokens(List<(char, string?)> tokens)
    {
        if (tokens.First().Item1 != '{')
        {
            tokens.Insert(0, ('{', null));
            tokens.Add(('}', null));
        }
        var contextStack = new Stack<ParseContext>();
        var context = new ParseContext
        {
            ReadId = READ_ROOT,
            WriteId = WRITE_ROOT,
            WriteMode = WriteMode.Read, //MoreWritesAtOrAfter(0, tokens) ? WriteMode.Write : WriteMode.Read,

        };
        var parseData = new ParseData
        {
            LoadedReadId = context.ReadId,
            LoadedWriteId = -2,
            IdCounter = 3,
            Ops = new List<ParseOperation>(),
            OpsMap = new Dictionary<OperatorKey, int> {
                { new OperatorKey(-1, ROOT, null, false), 1 }
            }
        };





        for (var i = 0; i < tokens.Count; i++)
        {
            var (token, accessor) = tokens[i];
            switch (token)
            {
                case '{':
                    parseData.Ops.Add(new ParseOperation(ParseOperationType.WriteSave, context.WriteId));
                    contextStack.Push(new ParseContext(context));
                    context.WriteMode = WriteMode.Read;
                    //context.WriteMode = MoreWritesAtOrAfter(i + 1, tokens) ? WriteMode.Write : WriteMode.Read;
                    break;
                case '}':
                    //ProcessWriteOps(ops, opsMap, ref idCounter, context, ref loadedWriteId, ref loadedReadId, writeOps);
                    if (!contextStack.TryPop(out var lastEntry))
                        throw new InvalidOperationException("un branching past start");
                    context = lastEntry;
                    break;
                case ':':
                    context.WriteMode = WriteMode.Write;
                    break;
                case '.':
                case '\'':
                case '"':
                case '[':
                    var acc = new AccessorData
                    {
                        Index = i,
                        Tokens = tokens,
                    };
                    if (acc.NextActiveChar() == ' ')
                        ProcessLastContextOperation(parseData, context, acc);
                    else if (context.WriteMode == WriteMode.Read)
                        ProcessReadOperator(parseData, context, acc);
                    else
                        ProcessWriteOperator(parseData, context, acc);

                    break;
                default:
                    break;
            }
        }



        /*
    if (context.WriteMode == WriteMode.Read)
        ops.Add(new ParseOperation(ParseOperationType.AddFromRead));


    if (context.WriteMode == WriteMode.Written)
        ops.Add(new ParseOperation(ParseOperationType.WriteFromRead));
    */

        //ProcessWriteOps(ops, opsMap, ref idCounter, context, ref loadedWriteId, ref loadedReadId, writeOps);

        //TODO remove unused saves

        var debug = parseData.Ops.Select(x => $"{x.OpType} {x.IntAcc} {x.StringAcc} ").Join("\n");



        return parseData.Ops;
    }




    private bool MoreWritesAtOrAfter(int i, List<(char, string?)> tokens)
    {
        var depth = 0;

        for (var j = i; j < tokens.Count; j++)
        {
            var (token, accessor) = tokens[j];

            switch (token)
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth < 0)
                        return false;
                    break;
                case ':':
                    if (depth == 0)
                        return true;
                    break;

            }
        }
        return false;
    }
}