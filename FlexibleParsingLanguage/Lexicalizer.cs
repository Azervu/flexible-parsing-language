using System.ComponentModel;

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

internal class QueryExpression
{
    private List<OperatorKey> _ops;

    public QueryExpression(List<OperatorKey> ops)
    {
        _ops = ops;
    }
}

internal class Lexicalizer
{
    public const char ROOT = '$';

    public const char BRANCH = '{';

    public const char UNBRANCH = '}';

    public const char SEPARATOR = ':';

    public const char ACCESS = '.';

    public const char WRITE = '_';

    internal Tokenizer Tokenizer { get; private set; }

    public Lexicalizer()
    {
        Tokenizer = new Tokenizer("${}:", '.', "'\"", '\\');
    }


    public Parser Lexicalize(string raw)
    {
        var tokens = Tokenizer.Tokenize(raw);
        var rawOps = ProcessTokens(tokens);
        var ops = ProcessRawOps(rawOps);

        return new Parser(ops);
    }

    private List<OperatorKey> ProcessTokens(List<(char, string?)> tokens)
    {
        var ops = new Dictionary<OperatorKey, int> {
            { new OperatorKey(-1, ROOT, null, false), 1 },
            { new OperatorKey(-1, ROOT, null, true), 2 }
        };

        var idStack = new Stack<(int, int, bool)>();

        var idCounter = 3;

        var readId = 1;
        var writeId = 2;
        var writeMode = false;

        var writeOps = new List<int>();




        foreach (var (token, accessor) in tokens)
        {
            switch (token)
            {
                case '{':
                    idStack.Push((readId, writeId, writeMode));
                    writeMode = false;
                    break;
                case '}':
                    var writeKey = new OperatorKey(writeId, WRITE, readId, true);
                    if (!ops.TryGetValue(writeKey, out writeId))
                    {
                        ops.Add(writeKey, ++idCounter);
                        writeOps.Add(idCounter);
                    }
                    if (!idStack.TryPop(out var lastEntry))
                        throw new InvalidOperationException("un branching past start");
                    readId = lastEntry.Item1;
                    writeId = lastEntry.Item2;
                    writeMode = lastEntry.Item3;
                    break;
                case ':':
                    writeMode = true;
                    break;
                default:
                    if (writeMode)
                    {
                        var key = new OperatorKey(writeId, token, accessor, writeMode);
                        if (!ops.TryGetValue(key, out writeId))
                        {
                            writeId = ++idCounter;
                            ops.Add(key, writeId);
                        }
                    }
                    else
                    {
                        var key = new OperatorKey(readId, token, accessor, writeMode);
                        if (!ops.TryGetValue(key, out readId))
                        {
                            readId = ++idCounter;
                            ops.Add(key, readId);
                        }
                    }
                    break;
            }
        }
        var writeKey2 = new OperatorKey(writeId, WRITE, readId, true);
        if (!ops.TryGetValue(writeKey2, out writeId))
        {
            ops.Add(writeKey2, ++idCounter);
            writeOps.Add(idCounter);
        }


        var references = ops.ToDictionary(x => x.Value, x => x.Key).ToDictionary();
        var refCount = ops.GroupBy(x => x.Key.TargetId).ToDictionary(x => x.Key, x => x.Count());
        foreach (var x in ops.Keys)
        {
            if (x.AccessorId == null)
                continue;

            if (refCount.TryGetValue((int)x.AccessorId, out var n))
                refCount[x.AccessorId] = n + 1;
            else
                refCount[x.AccessorId] = 1;
        }
        var multiRef = refCount.Where(x => x.Value > 1).Select(x => x.Key).ToHashSet();

        var includedOperations = new HashSet<int>();    
        var orderedOperations = new List<OperatorKey>();
        foreach (var write in writeOps)
        {
            var op = references[write];
            var a = op.TargetId;
            var i = orderedOperations.Count;

            while (!includedOperations.Contains(a) && a != -1)
            {
                var o = references[a];
                orderedOperations.Insert(i, o);
                includedOperations.Add(a);

                if (multiRef.Contains(a))
                {
                    //TODO save operation
                }

                a = o.TargetId;
            }

            if (a != -1)
            {
                //TODO load operation
                //orderedOperations.Insert(i, o);
            }


            a = (int)op.AccessorId;
            while (!includedOperations.Contains(a) && a != -1)
            {
                var o = references[a];
                orderedOperations.Insert(i, o);
                includedOperations.Add(a);

                if (multiRef.Contains(a))
                {
                    //TODO save operation
                }

                a = o.TargetId;
            }
        }

        return orderedOperations;
    }





    private List<ParseOperation> ProcessRawOps(List<OperatorKey> ops)
    {
        var result = new List<ParseOperation>();
        var writeInited = false;

        foreach (var op in ops)
        {


            if (op.Write)
            {
                switch (op.Operator)
                {
                    case '$':
                        if (writeInited)
                        {
                            result.Add(new ParseOperation(ParseOperationType.WriteRoot));
                        }
                        else
                        {
                            writeInited = true;
                            result.Add(new ParseOperation(ParseOperationType.WriteInitRoot));
                        }
                        break;
                    case '.':
                        if (op.Accessor == null)
                            result.Add(new ParseOperation(ParseOperationType.WriteAccessInt, op.AccessorId));
                        else
                            result.Add(new ParseOperation(ParseOperationType.WriteAccess, op.Accessor));
                        break;
                    default:
                        throw new InvalidOperationException(op.Operator + " not handled write");
                }
            }
            else
            {
                switch (op.Operator)
                {
                    case '$':
                        result.Add(new ParseOperation(ParseOperationType.ReadRoot));
                        break;
                    case '.':
                        if (op.Accessor == null)
                            result.Add(new ParseOperation(ParseOperationType.ReadAccessInt, op.AccessorId));
                        else
                            result.Add(new ParseOperation(ParseOperationType.ReadAccess, op.Accessor));
                        break;
                    default:
                        throw new InvalidOperationException(op.Operator + " not handled read");
                }
            }
        }

        return result;
    }





}




