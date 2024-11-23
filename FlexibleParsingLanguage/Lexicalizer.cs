using System.ComponentModel;
using System.Diagnostics.Metrics;

namespace FlexibleParsingLanguage;

internal struct GroupToken
{
    internal readonly char Start;
    internal readonly char End;

    public GroupToken(char start, char end)
    {
        Start = start;
        End = end;
    }
}

internal struct OperatorToken
{
    internal readonly char Op;
    internal readonly int Rank;
    internal readonly bool Postfix;
    internal readonly bool Prefix;

    public OperatorToken(char op, int rank, bool pre, bool post)
    {
        Op = op;
        Rank = rank;
        Postfix = pre;
        Prefix = post;
    }
}

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
        var ops = ProcessTokens(tokens);
        return new Parser(ops);
    }

    private List<ParseOperation> ProcessTokens(List<(char, string?)> tokens)
    {
        var opsMap = new Dictionary<OperatorKey, int> {
            { new OperatorKey(-1, ROOT, null, false), 1 }
        };

        var idStack = new Stack<(int, int, bool)>();

        var idCounter = 3;

        var readId = READ_ROOT;
        var writeId = WRITE_ROOT;
        var loadedReadId = readId;
        var loadedWriteId = -2;
        var writeMode = StartWriteMode(0, tokens);
        var ops = new List<ParseOperation>();

        var writeOps = new List<(char, string?)>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var (token, accessor) = tokens[i];
            switch (token)
            {
                case '{':
                    ops.Add(new ParseOperation(ParseOperationType.WriteSave, writeId));
                    idStack.Push((readId, writeId, writeMode));
                    writeMode = StartWriteMode(i + 1, tokens);
                    break;
                case '}':
                    ops.Add(new ParseOperation(ParseOperationType.WriteSave, writeId));
                    ProcessWriteOps(ops, opsMap, ref idCounter, ref writeId, ref readId, ref loadedWriteId, ref loadedReadId, writeOps);
                    if (!idStack.TryPop(out var lastEntry))
                        throw new InvalidOperationException("un branching past start");
                    readId = lastEntry.Item1;
                    writeId = lastEntry.Item2;
                    writeMode = lastEntry.Item3;
                    break;
                case ':':
                    writeMode = false;
                    break;
                case '.':
                case '\'':
                case '"':
                case '[':

                    if (writeMode)
                        writeOps.Add((token, accessor));
                    else
                        ProcessReadOps(ops, opsMap, ref idCounter, ref readId, ref loadedReadId, token, token != '[', accessor);
                    break;
                default:
                    break;
            }
        }

        ProcessWriteOps(ops, opsMap, ref idCounter, ref writeId, ref readId, ref loadedWriteId, ref loadedReadId, writeOps);

        //TODO remove unused saves

        return ops;
    }




    private bool StartWriteMode(int i, List<(char, string?)> tokens)
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