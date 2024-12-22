namespace FlexibleParsingLanguage.Compiler.Util;

internal partial class Lexicalizer
{
    internal List<OpConfig> Ops { get; private set; }

    private OpConfig DefaultOp { get; set; }
    private OpConfig RootOp { get; set; }
    internal const int RootOpId = 1;


    private OpConfig AccessorOp { get; set; } = new OpConfig(null, OpCategory.Accessor, 99);

    private string UnescapeToken { get; set; }


    private Dictionary<string, OpConfig?> Operators = new();

    public Lexicalizer(List<OpConfig> ops)
    {
        Ops = ops;
        foreach (var op in ops)
        {
            HandleConfigEntry(op.Operator, op);

            if (op.GroupOperator != null)
            {
                var op2 = op.GroupOperator.ToString();
                HandleConfigEntry(op2, new OpConfig(op2, OpCategory.UnGroup, -100));
            }


            if (op.Category.Has(OpCategory.Default))
                DefaultOp = op;

            if (op.Category.Has(OpCategory.Root))
                RootOp = op;

            if (op.Category.Has(OpCategory.Unescape))
                UnescapeToken = op.Operator;
        }
        if (DefaultOp == null)
            throw new Exception("Default operator missing");

        if (RootOp == null)
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
        var tokens = Tokenize(raw).ToList();
        var ops = ProcessTokens(tokens);


        var tt = tokens.Select(x => $"{x.Op?.Operator ?? ($"'{x.Accessor}'")}").Join("\n");
        var t = ops.Select(x => $"({x.Id}){x.Type.Operator} '{x.Accessor}'").Join("\n");


        Sequence(ref ops);

        foreach (var op in ops)
        {
            if (op.Type != AccessorOp && op.Accessor != null)
                throw new InvalidOperationException("Accessor on non-accessor operation");
        }

        return ops;
    }

    private List<RawOp> ProcessTokens(List<Token> tokens)
    {
        var ops = new List<RawOp>(tokens.Count) {
            new RawOp {
                Id = 1,
                CharIndex = -1,
                Type = RootOp,
            }
        };
        var idCounter = 2;

        RawOp? op = null;


        foreach (var t in tokens)
        {
            var hadOp = false;

            if (op != null)
            {
                if (op.Type != DefaultOp) {
                    hadOp = true;
                    op.Id = idCounter++;
                    ops.Add(op);
                }
                op = null;
            }

            if (t.Op != null)
            {
                op = new RawOp
                {
                    CharIndex = t.Index,
                    Type = t.Op,
                };
            }
            else
            {
                if (!hadOp)
                {
                    ops.Add(new RawOp
                    {
                        Id = idCounter++,
                        CharIndex = t.Index,
                        Type = DefaultOp,
                    });
                }

                ops.Add(new RawOp
                {
                    Id = idCounter++,
                    CharIndex = t.Index,
                    Type = AccessorOp,
                    Accessor = t.Accessor,
                });
            }
        }

        if (op != null && op.Type != DefaultOp)
        {
            op.Id = idCounter++;
            ops.Add(op);
        }

        return ops;
    }
}