﻿using static System.Runtime.InteropServices.JavaScript.JSType;

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

            (ParseOperation, OperatorKey)? opKey = null;

            switch (accessor.Operator)
            {
                case ':':
                    ctx.WriteMode = WriteMode.Write;
                    break;
                case '*':
                    if (ctx.WriteMode == WriteMode.Read)
                        opKey = (new ParseOperation(ParseOperationType.ReadFlatten, accessor.Accessor), new OperatorKey(ctx.ActiveId, accessor.Operator, accessor.Accessor, false));
                    else
                        ProcessWriteFlattenOperator(i, config, parser, ctx, accessor);
                    break;
                case '~':
                    if (ctx.WriteMode == WriteMode.Read)
                        parser.Ops.Add(new ParseOperation(ParseOperationType.ReadName));
                    else
                        parser.Ops.Add(new ParseOperation(ParseOperationType.WriteNameFromRead));


                    //ctx.ActiveId = ++parser.IdCounter;
                    //parser.SaveOps.Add(ctx.ActiveId);
                    //parser.Ops.Add(new ParseOperation(ParseOperationType.Save, ctx.ActiveId));

                    break;
                case '.':
                case '\'':
                case '"':
                case '[':
                    if (ctx.WriteMode == WriteMode.Read)
                    {
                        opKey = (new ParseOperation(ParseOperationType.Read, accessor.Accessor), new OperatorKey(ctx.ActiveId, accessor.Operator, accessor.Accessor, false));
                    }
                    else if (i == ctx.Accessors.Count - 1)
                    {
                        processedEnd = true;
                        ProcessContextEndingOperator(config, parser, ctx, accessor);
                    }
                    else
                    {
                        ProcessWriteOperator(i, config, parser, ctx, accessor);
                    }
                    break;
                default:
                    break;
            }




        /*
             





        var opId = ++parser.IdCounter;
        var op = new ParseOperation(ParseOperationType.ReadFlatten, data.Accessor);

        var key = new OperatorKey(ctx.ActiveId, data.Operator, data.Accessor, false);
        if (parser.OpsMap.TryGetValue(key, out var readId))
        {
            ctx.ActiveId = readId;
            return;
        }

        EnsureReadOpLoaded(parser, ctx);
        ctx.ActiveId = opId;
        parser.SaveOps.Add(ctx.ActiveId);
        parser.LoadedId = ctx.ActiveId;
        parser.OpsMap.Add(key, ctx.ActiveId);
        parser.Ops.Add(op);
        parser.Ops.Add(new ParseOperation(ParseOperationType.Save, ctx.ActiveId));

        */




            if (opKey != null)
            {
                var (op, key) = opKey.Value;

                if (parser.OpsMap.TryGetValue(key, out var readId))
                {
                    ctx.ActiveId = readId;
                }
                else
                {
                    EnsureReadOpLoaded(parser, ctx);
                    ctx.ActiveId = ++parser.IdCounter;
                    parser.SaveOps.Add(ctx.ActiveId);
                    parser.LoadedId = ctx.ActiveId;
                    parser.OpsMap.Add(key, ctx.ActiveId);
                    parser.Ops.Add(op);
                    parser.Ops.Add(new ParseOperation(ParseOperationType.Save, ctx.ActiveId));
                }
            }

        }



        if (!processedEnd && ctx.WriteMode == WriteMode.Read)
            ProcessContextEndingOperator(config, parser, ctx, null);
    }

    private void ProcessContextEndingOperator(ParserConfig config, ParseData data, ParseContext ctx, AccessorData? acc)
    {

        if (ctx.Accessors.Count != 0 && ctx.Accessors.Last().Ctx != null)
            return;

        EnsureReadOpLoaded(data, ctx);
        EnsureWriteOpLoaded(config, data, ctx, acc);

        if (acc == null || ctx.WriteMode == WriteMode.Read)
            data.Ops.Add(new ParseOperation(ParseOperationType.WriteAddRead));
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

 
        ctx.ActiveId = data.LoadedId;
    }
}