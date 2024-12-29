using FlexibleParsingLanguage.Operations;
using FlexibleParsingLanguage.Parse;

namespace FlexibleParsingLanguage.Compiler;

internal partial class Compiler
{
    internal List<OpConfig> Ops { get; private set; }

    private OpConfig DefaultOp { get; set; }

    internal const int RootId = 2;
    private OpConfig ParamOperator { get; set; }

    internal const int RootGroupId = 1;

    internal readonly OpConfig RootOperator;

    private string UnescapeToken { get; set; }

    private Dictionary<string, OpConfig?> Operators = new();

    internal Compiler(List<OpConfig> ops)
    {
        Ops = ops;
        foreach (var op in ops)
        {
            HandleConfigEntry(op.Operator, op);

            if (op.GroupOperator != null)
            {
                var op2 = op.GroupOperator.ToString();
                HandleConfigEntry(op2, new OpConfig(op2, OpSequenceType.UnGroup, null, -100));
            }

            if (op.SequenceType.All(OpSequenceType.Default))
            {
                DefaultOp = op;
            }

            if (op.SequenceType.All(OpSequenceType.RootParam))
                ParamOperator = op;

            if (op.SequenceType.All(OpSequenceType.Unescape))
                UnescapeToken = op.Operator;

            if (op.SequenceType.All(OpSequenceType.Root))
                RootOperator = op;
        }
        if (DefaultOp == null)
            throw new Exception("Default operator missing");

        if (ParamOperator == null)
            throw new Exception("Root operator missing");

    }

    private void HandleConfigEntry(string op, OpConfig? config)
    {
        for (var i = 0; i < op.Length - 1; i++)
        {
            var o = op.Substring(i, i + 1);
            if (!Operators.ContainsKey(o))
                Operators[o] = null;
        }
        if (!Operators.TryGetValue(op, out var v) || v == null)
            Operators[op] = config;
    }

    internal List<RawOp> Lexicalize(string raw)
    {

        try
        {
            var tokens = Tokenize(raw).ToList();
            var ops = ProcessTokens(tokens);

#if DEBUG
            var tt = tokens.Select(x => $"{x.Op?.Operator ?? ($"'{x.Accessor}'")}").Join("\n");
            var t = ops.Select(x => $"({x.Id,2}){(string.IsNullOrEmpty(x.Accessor) ? x.Type.Operator : $"'{x.Accessor}'"),5}").Join("\n");
#endif

            Sequence(ref ops);


            foreach (var op in ops)
            {
                if (op.Type != FplOperation.Accessor && op.Accessor != null)
                    throw new InvalidOperationException("Accessor on non-accessor operation");
            }

            return ops;

        } catch (QueryCompileException ex)
        {
            ex.Query = raw;
            throw;
        }
    }

    internal FplQuery Compile(string raw, ParsingMetaContext configContext)
    {
        var ops = Lexicalize(raw);

        var rootId = 1;
        var parseData = new ParseData
        {
            ActiveId = rootId,
            LoadedId = rootId,
            IdCounter = 3,
            Ops = [],
            SaveOps = [],
            OpsMap = new Dictionary<(int LastOp, ParseOperation[]), int>
            {
            },
        };

        OpCompileType rootType = OpCompileType.None;

        var compiled = ops
            .Where(x => x.Type.Compile != null)
            .SelectMany(x =>
            {
                if (x.Type.Compile == null)
                    throw new QueryCompileException(x, "missing compiler function", true);

                if (rootType == OpCompileType.None)
                    rootType = x.Type.CompileType;

                return x.Type.Compile(parseData, x);
            }).Where(x => x != null).ToList();

        if (rootType == OpCompileType.None)
            rootType = OpCompileType.WriteArray;

        return new FplQuery(compiled, configContext, new ParserRootConfig { RootType = rootType });
    }

    private List<RawOp> ProcessTokens(List<Token> tokens)
    {
        var ops = new List<RawOp>(tokens.Count) {
            new RawOp {
                Id = RootGroupId,
                CharIndex = -1,
                Type = RootOperator,
            },
        };

        var idCounter = 2;
        bool checkedRoot = false;
        RawOp? op = null;
        foreach (var t in tokens)
        {
            RawOp? accessor = null;
            if (t.Op == null || t.Op.SequenceType.All(OpSequenceType.Accessor))
            {
                accessor = new RawOp
                {
                    Id = idCounter++,
                    CharIndex = t.Index,
                    Type = t.Op ?? FplOperation.Accessor,
                    Accessor = t.Accessor,
                };

                if (op != null && !op.Type.SequenceType.All(OpSequenceType.RightInput))
                {
                    ops.Add(op);
                    op = null;
                }

                if (op == null)
                {
                    op = new RawOp
                    {
                        Id = idCounter++,
                        CharIndex = t.Index,
                        Type = DefaultOp,
                    };
                }
            }
            else
            {
                if (t.Op == DefaultOp)
                    continue;

                if (op != null && op.Type != DefaultOp)
                    ops.Add(op);

                op = new RawOp
                {
                    Id = idCounter++,
                    CharIndex = t.Index,
                    Type = t.Op,
                };
            }


            if (!checkedRoot)
            {
                checkedRoot = true;
                if (op.Type.SequenceType.All(OpSequenceType.LeftInput))
                {
                    ops.Add(new RawOp
                    {
                        Id = idCounter++,
                        CharIndex = t.Index,
                        Type = ParamOperator,
                    });
                }
            }
            if (accessor != null)
            {
                ops.Add(op);
                ops.Add(accessor);
                op = null;
                accessor = null;
            }
        }

        if (op != null)
            ops.Add(op);

        if (RootOperator.SequenceType.All(OpSequenceType.Group))
        {
            ops.Add(new RawOp
            {
                Id = idCounter++,
                CharIndex = -1,
                Type = Operators[RootOperator.GroupOperator],
            });
        };

        return ops;
    }
}