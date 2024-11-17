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

    private Dictionary<char, GroupToken> GroupTokens { get; set; }
    private Dictionary<char, OperatorToken> OperatorTokens { get; set; }

    private HashSet<char> MetadataTokens { get; set; }
    private HashSet<char> TerminatorTokens { get; set; }
    private HashSet<char> GroupingEndTokens { get; set; }

    internal class TempOp
    {
        internal TempOp? Parent { get; set; }
        internal List<TempOp> Children { get; set; }

        internal char Op { get; set; }
        internal string? Acc { get; set; }


        internal int WriteId { get; set; }
        internal int ReadId { get; set; }

        internal TempOp(TempOp? parent, char op, string? acc)
        {
            Parent = parent;
            Op = op;
            Acc = acc;
            Children = [];
            ReadId = op == ROOT ? 1 : 0;
            WriteId = op == ROOT ? -1 : 0;
        }
    }



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


        /*


        var temp = new Dictionary<int, List<(char, string?)>> { };
        var parseOps = new Dictionary<OperatorKey, int> { { new OperatorKey(0, '$', null), 0 } };

        var writeOps = new Dictionary<int, int> { };



        var activeId = 0;
        var idCounter = 0;
        var tempIdCounter = 0;

        for (int i = 0; i < raw.Length; i++)
        {
            var (t, acc) = tokens[i];
            if (GroupTokens.TryGetValue(t, out var grp))
            {
                var n = 1;
                var o = new List<(char, string?)>();
                while (true)
                {
                    i++;
                    var (t2, acc2) = tokens[i];

                    if (grp.Start == t2)
                        n++;
                    else if (grp.End == t2)
                        n--;

                    if (n == 0)
                        break;

                    o.Add((t2, acc2));
                }


                //TODO

                continue;
            }


            var key = new OperatorKey(activeId, t, acc);
            if (!parseOps.TryGetValue(key, out activeId))
            {
                activeId = ++idCounter;
                parseOps.Add(key, activeId);
            }


        }






        foreach (var (opr, acc) in tokens)
        {
        }






            //var expressions = tokens.Select(x => x)




            //a@b




            var expressions = new List<QueryExpression>();
        var expressionStack = new Stack<(char, List<QueryExpression>)>();


        foreach (var (opr, acc) in tokens)
        {

            var active = expressionStack.TryPeek(out var activeGroup)
                ? activeGroup.Item2
                : expressions;

            if (expressionStack.TryPeek(out var stackEntry) && stackEntry.Item1 == opr)
            {
                UpdateExpression(stackEntry.Item2);
                expressionStack.Pop();
                continue;
            }

            if (opr is char op1 && GroupingEndTokens.Contains(op1))
                throw new Exception($"mismatched group token '{op1}'");

            var exp = new QueryExpression(opr, acc);
            active.Add(exp);

            if (opr is char op)
            {
                if (GroupTokens.TryGetValue(op, out var groupToken))
                {
                    expressionStack.Push((groupToken.End, exp.Children));
                }
            }


            var s = 345543;

        }


        UpdateExpression(expressions);

        if (expressionStack.Count != 0)
            throw new Exception($"unclosed group");

        //if (expressions.Count != 1)
        //      throw new Exception($"unclosed group");


        var txt = "";
        foreach (var e in expressions)
            AppendData(ref txt, e, 0);


        return expressions.FirstOrDefault();
        */


    }






    private List<ParseOperation> ProcessRawOps(List<OperatorKey> ops)
    {
        var result = new List<ParseOperation>();
        var writeInited = false;

        foreach (var op in ops) {


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
                refCount[(int)x.AccessorId] = n + 1;
            else
                refCount[(int)x.AccessorId] = 1;
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





    private TempOp GroupOps(List<(char, string?)> tokens)
    {


        var root = new TempOp(null, 'Æ', null);
        var active = root;


        if (tokens.Count > 0 && tokens[0].Item1 != BRANCH)
        {
            tokens.Insert(0, (BRANCH, null));
            tokens.Add((UNBRANCH, null));
        }

        foreach (var token in tokens)
        {
            switch (token.Item1)
            {
                case BRANCH:
                    active.Children.Add(new TempOp(active, token.Item1, token.Item2));
                    active = active.Children.Last();
                    break;
                case UNBRANCH:
                    if (active.Parent == null)
                        throw new Exception("unbranch past root");
                    active = active.Parent;
                    break;
                case SEPARATOR:
                    if (active.Op != BRANCH)
                        throw new Exception("separator set on non branch context");
                    var op = active.Op;
                    active.Op = token.Item1;
                    active = active.Parent;
                    active.Children.Add(new TempOp(active, op, token.Item2));
                    active = active.Children.Last();


                    break;
                default:
                    active.Children.Add(new TempOp(active, token.Item1, token.Item2));
                    break;
            }
        }
        return root;
    }

    private (Dictionary<OperatorKey, int>, Dictionary<OperatorKey, int>) ProcessGroupedTemp(TempOp root)
    {
        var indices = new List<int> { 0 };
        var active = root;

        var readIdCounter = 1;
        var writeIdCounter = -1;

        var readOps = new Dictionary<OperatorKey, int> {
            { new OperatorKey(0, ROOT, null), 1 }
        };
        var writeOps = new Dictionary<OperatorKey, int> {
            { new OperatorKey(0, ROOT, null), -1 }
        };



        if (root.Children.FirstOrDefault()?.Op != ROOT)
            root.Children.Insert(0, new TempOp(null, ROOT, null));


        root.ReadId = -3;

        while (indices.Count > 0)
        {
            var depth = indices.Count - 1;
            var i = indices[depth];
            if (i >= active.Children.Count)
            {
                indices.RemoveAt(indices.Count - 1);
                continue;
            }
            var c = active.Children[i];



            if (c.Op == SEPARATOR)
            {

            }


            if (c.Children.Count > 0)
            {
                indices.Add(0);
                active = c;
            }
            else
            {
                OperatorKey key;
                if (c.Op == ROOT)
                {
                    key = new OperatorKey(0, ROOT, null);
                }
                else
                {
                    var readId = -1;
                    for (var d = depth; d >= 0; d--)
                    {
                        var ii = indices[d];
                        if (ii > 0)
                        {
                            readId = active.Children[ii - 1].ReadId;
                            break;
                        }
                    }
                    key = new OperatorKey(readId, c.Op, c.Acc);
                }

                if (!readOps.TryGetValue(key, out var id))
                {
                    c.ReadId = ++readIdCounter;
                    readOps.Add(key, c.ReadId);
                }
            }

            indices[indices.Count - 1] = i + 1;
        }


        var read = readOps.Select(x => $"{x.Value}: {x.Key.TargetId}{x.Key.Operator}{x.Key.Accessor}").Join("\n");



        /*
        
         "k:$.a"
         
0: 0$
1: -2.a

         */



        return (readOps, writeOps);
    }






    /*




    private void AppendData(ref string txt, QueryExpression exp, int depth)
    {
        txt += $"\n{new string(' ', 2 * depth)}|{exp.Op}|{exp.Accessor}";
        foreach (var c in exp.Children)
        {
            AppendData(ref txt, c, depth + 1);
        }
    }


    private void UpdateExpression(List<QueryExpression> expressions)
    {
        while (expressions.Count > 1)
        {
            var maxRank = int.MinValue;
            var index = -1;

            for (var i = 0; i < expressions.Count; i++)
            {
                var exp = expressions[i];

                if (exp.Proccessed)
                    continue;

                if (exp.Op is char op && OperatorTokens.TryGetValue(op, out var v))
                {
                    if (v.Rank > maxRank)
                    {
                        maxRank = v.Rank;
                        index = i;
                    }
                }
            }

            if (index == -1)
                break;

            var exp2 = expressions[index];
            var o = OperatorTokens[(char)exp2.Op];

            if (o.Prefix)
            {
                if (index >= expressions.Count - 1)
                    throw new Exception("Last operator is prefix");
                exp2.Children.Add(expressions[index + 1]);
                expressions.RemoveAt(index + 1);
            }

            if (o.Postfix)
            {
                if (index == 0)
                    throw new Exception("Last operator is prefix");

                exp2.Children.Insert(0, expressions[index - 1]);
                expressions.RemoveAt(index - 1);
            }

            exp2.Proccessed = true;
        }
    }

    */

}




