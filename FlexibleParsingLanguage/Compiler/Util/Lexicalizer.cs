using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler.Util;

internal class RawOp
{
    internal OpConfig? Type { get; set; }
    internal string? Accessor { get; set; }

    internal List<RawOp> Input { get; set; } = new List<RawOp>();
    internal List<RawOp> Output { get; set; } = new List<RawOp>();

    internal uint Order { get; set; }


    internal List<RawOp>? Children { get; set; }


    internal void AddLog(StringBuilder log, int depth)
    {
        log.Append($"\n{new string(' ', 4 * depth)}");
        if (Type?.Operator != null)
        {
            log.Append(Type?.Operator);

            if (Accessor != null)
                log.Append($"  ");
        }
        if (Accessor != null)
            log.Append($"\"{Accessor}\"");

        if (Children != null)
        {
            foreach (var c in Children)
                c.AddLog(log, depth + 1);
        }
    }

    internal string ToString2()
    {
        var l = new StringBuilder();
        AddLog(l, 0);
        return l.ToString();
    }
}

internal partial class Lexicalizer
{
    private string DefaultOp { get; set; }
    private char UnescapeToken { get; set; }

    private OpConfig UnknownOp { get; set; } = new OpConfig(null, OpCategory.Unknown);

    private Dictionary<string, OpConfig?> Operators = new();

    public Lexicalizer(
        List<OpConfig> ops,
        string defaultOperator,
        char unescapeToken
    )
    {

        foreach (var op in ops)
        {
            HandleConfigEntry(op.Operator, op);

            if (op.GroupOperator != null)
            {
                var op2 = op.GroupOperator.ToString();
                HandleConfigEntry(op2, new OpConfig(op2, OpCategory.UnBranch));
            }


        }
        DefaultOp = defaultOperator;
        UnescapeToken = unescapeToken;
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


        var defaultOp = Operators[DefaultOp] ?? throw new Exception("Default operator missing");

        tokens = tokens.Select(x => {

            if (x.Item1 != UnknownOp)
                return x;
            return (defaultOp, x.Item2);
        }).ToList();



        var ops = new List<RawOp>(tokens.Count);
        for (var i = 0; i < tokens.Count; i++)
        {
            var (op, acc) = tokens[i];
            ops.Add(new RawOp
            {
                Type = op,
                Accessor = acc,
                Order = (uint)i,
            });
        }



        Sequencer.Sequence(ops);



        return (GroupTokens(tokens), ops);
    }

    private IEnumerable<(OpConfig, string?)> Tokenize(string raw)
    {
        using (var it = new CharEnumerator(raw))
        {
            if (!it.MoveNext())
                yield break;
            while (it.Valid)
            {
                var cc = it.Current;
                var active = it.Current.ToString();
                if (!Operators.TryGetValue(active, out var op))
                {
                    while (it.MoveNext())
                    {
                        var c = it.Current.ToString();
                        if (Operators.ContainsKey(c))
                            break;
                        active += c;
                    }
                    yield return (UnknownOp, active);
                    continue;
                }

                while (it.MoveNext())
                {
                    var nextActive = active + it.Current;
                    if (!Operators.TryGetValue(nextActive, out var op2))
                        break;
                    active = nextActive;

                    op = op2;
                }

                if (op.Category.Has(OpCategory.Literal))
                {
                    active = string.Empty;
                    while (true)
                    {
                        if (it.Current == op.GroupOperator)
                        {
                            it.MoveNext();
                            break;
                        }
                        if (it.Current == UnescapeToken && !it.MoveNext())
                            break;
                        active += it.Current;
                        if (!it.MoveNext())
                            break;
                    }
                    yield return (op, active);
                }
                else if (op.Operator != DefaultOp)
                {
                    yield return (op, null);
                }
            }
        }
    }

    private List<RawOp> GroupTokens(List<(OpConfig, string?)> tokens)
    {
        var stack = new List<RawOp> { new() { Type = new OpConfig(null, OpCategory.Temp), Children = [] } };
        RawOp? prefixOp = null;

        for (var i = 0; i < tokens.Count(); i++)
        {
            var (op, acc) = tokens[i];

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
                Order = (uint)i,
            };

            if (addToPrefix)
            {
                if (op.Operator == DefaultOp && prefixOp.Accessor == null)
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
