using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static FlexibleParsingLanguage.Compiler.Util.Lexicalizer;

namespace FlexibleParsingLanguage.Compiler.Util;

internal partial class Lexicalizer
{
    internal List<OpConfig> Ops { get; private set; }

    private OpConfig DefaultOp { get; set; }
    private OpConfig RootOp { get; set; }
    internal const int RootOpId = 1;


    private OpConfig AccessorOp { get; set; } = new OpConfig(null, OpCategory.Accessor, 99);

    private char UnescapeToken { get; set; }


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
                UnescapeToken = op.Operator[0];
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

    internal (List<RawOp>, List<RawOp>) Lexicalize(string raw)
    {
        var tokens = Tokenize(raw).ToList();
        var ops = ProcessTokens(tokens);
        Sequence(ref ops);

        foreach (var op in ops)
        {
            if (op.Type != AccessorOp && op.Accessor != null)
                throw new InvalidOperationException("Accessor on non-accessor operation");
        }

        tokens = tokens.Select(x => {

            if (x.Item1 != AccessorOp)
                return x;
            return (DefaultOp, x.Item2, x.Item3, x.Item4);
        }).ToList();


        return (GroupTokens(tokens), ops);
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

        foreach (var t in tokens)
        {
            if (t.Op == DefaultOp && string.IsNullOrEmpty(t.Accessor))
                continue;

            var op = new RawOp
            {
                CharIndex = t.OpIndex,
                Type = t.Op,
            };
            ops.Add(op);
            if (string.IsNullOrEmpty(t.Accessor))
            {
                op.Id = idCounter++;
                continue;
            }

            if (t.Op.Category.Has(OpCategory.Prefix))
            {
                op.Prefixed = true;
                op.Input.Add(new RawOp
                {
                    Id = idCounter++,
                    CharIndex = t.AccessorIndex,
                    Type = AccessorOp,
                    Accessor = t.Accessor,
                });
                op.Id = idCounter++;
            }
            else
            {
                op.Id = idCounter++;
                var defaultOp = new RawOp
                {
                    Id = idCounter++,
                    CharIndex = t.AccessorIndex,
                    Type = DefaultOp,
                    Prefixed = true
                };
                defaultOp.Input.Add(new RawOp
                {
                    Id = idCounter++,
                    CharIndex = t.AccessorIndex,
                    Type = AccessorOp,
                    Accessor = t.Accessor,
                });
                ops.Add(defaultOp);
            }
        }




        return ops;
    }



























    private List<RawOp> GroupTokens(List<(OpConfig, int, string?, int)> tokens)
    {
        var stack = new List<RawOp> { new() { Type = new OpConfig(null, OpCategory.Temp), Children = [] } };
        RawOp? prefixOp = null;

        for (var i = 0; i < tokens.Count(); i++)
        {
            var (op, opIndex, acc, charIndex) = tokens[i];

            var addToPrefix = prefixOp != null;

            var groupOp = stack[stack.Count - 1];
            if (stack.Count > 1 && op.Operator == groupOp.Type.GroupOperator.ToString())
            {
                if (addToPrefix)
                    throw new InvalidOperationException("Ungrouping is prefix param");
                stack.RemoveAt(stack.Count - 1);
                continue;
            }

            var c = new RawOp
            {
                Type = op,
                Accessor = acc,
            };

            if (addToPrefix)
            {
                if (op.Operator == DefaultOp.Operator && prefixOp.Accessor == null)
                    prefixOp.Accessor = acc;
                else
                    prefixOp.Children = new List<RawOp> { c };
                prefixOp = null;
            }
            else
            {
                groupOp.Children.Add(c);
            }


            if (c.Type.GroupOperator != null)
            {
                c.Children = new List<RawOp>();
                stack.Add(c);
            }

            if (c.Type?.Category.Has(OpCategory.Prefix) == true)
            {
                if (c.Accessor == null)
                    prefixOp = c;
            }
        }

        if (prefixOp != null)
            throw new InvalidOperationException("Prefix lacks param");
        return stack[0].Children;
    }
}