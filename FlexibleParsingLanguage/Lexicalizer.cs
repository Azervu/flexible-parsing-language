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
        //var ops = ProcessRawOps(rawOps);

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

                    /*
                    var writeKey = new OperatorKey(writeId, WRITE, readId, true);
                    if (!opsMap.TryGetValue(writeKey, out writeId))
                    {
                        opsMap.Add(writeKey, ++idCounter);
                        ops.Add(new ParseOperation(ParseOperationType.WriteFromRead));
                    }
                    */



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


        /*
        foreach (var (token, accessor) in tokens)
        {
            switch (token)
            {
                case '{':
                    idStack.Push((readId, writeId, writeMode));
                    writeMode = true;
                    break;
                case '}':

                    if (writeOps2.Any())
                        ProcessWriteOps(ops, opsMap, ref idCounter, ref writeId, ref readId, ref loadedWriteId, ref loadedReadId, writeOps2);


                    var writeKey = new OperatorKey(writeId, WRITE, readId, true);
                    if (!opsMap.TryGetValue(writeKey, out writeId))
                    {
                        opsMap.Add(writeKey, ++idCounter);
                        writeOps.Add(idCounter);
                        ops.Add(new ParseOperation(ParseOperationType.WriteFromRead));
                    }
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
                        writeOps2.Add((token, accessor));
                    else
                        ProcessReadOps(ops, opsMap, ref idCounter, ref writeId, ref readId, ref loadedWriteId, ref loadedReadId, token, token != '[', accessor);
                    break;
                default:
                    break;
            }
        }
        */

        ProcessWriteOps(ops, opsMap, ref idCounter, ref writeId, ref readId, ref loadedWriteId, ref loadedReadId, writeOps);


        return ops;


        /*

        var writeKey2 = new OperatorKey(writeId, WRITE, readId, true);
        if (!opsMap.TryGetValue(writeKey2, out writeId))
        {
            opsMap.Add(writeKey2, ++idCounter);
            writeOps.Add(idCounter);
        }


        var references = opsMap.ToDictionary(x => x.Value, x => x.Key).ToDictionary();
        var refCount = opsMap.GroupBy(x => x.Key.TargetId).ToDictionary(x => x.Key, x => x.Count());
        foreach (var x in opsMap.Keys)
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
        */
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




