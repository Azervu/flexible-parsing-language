using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler.Util;

internal class TokenGroup
{
    internal OpConfig? Op { get; set; }
    internal string? Accessor { get; set; }

    internal uint Order { get; set; }

    internal List<TokenGroup>? Children { get; set; }
    internal void AddLog(StringBuilder log, int depth)
    {
        log.Append($"\n{new string(' ', 4 * depth)}");


        if (Op?.Operator != null)
        {
            log.Append(Op?.Operator);

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

internal class Lexicalizer
{
    private string DefaultOp { get; set; }
    private char UnescapeToken { get; set; }

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
            if (op.EndOperator != null && op.Type == OpTokenType.Group)
            {
                var op2 = op.EndOperator.ToString();
                HandleConfigEntry(op2, new OpConfig(op2, OpTokenType.Temp));
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

    internal List<TokenGroup> Lexicalize(string raw)
    {
        var tokens = Tokenize(raw).ToList();
        var groupedTokens = GroupTokens(tokens);
        return groupedTokens;
    }


    private IEnumerable<(OpConfig, string?)> Tokenize(string raw)
    {
        var defaultOp = Operators[DefaultOp] ?? throw new Exception("Default operator missing");
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
                    yield return (defaultOp, active);
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

                if (op.Value.Type == OpTokenType.Escape)
                {
                    active = string.Empty;
                    while (true)
                    {
                        if (it.Current == op.Value.EndOperator)
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
                    yield return ((OpConfig)op, active);
                }
                else if (op.Value.Operator != DefaultOp)
                {
                    yield return ((OpConfig)op, null);
                }
            }
        }
    }

    private List<TokenGroup> GroupTokens(List<(OpConfig, string?)> tokens)
    {
        var stack = new List<TokenGroup> { new TokenGroup { Op = new OpConfig { Operator = null }, Children = [] } };
        TokenGroup? prefixOp = null;

        for (var i = 0; i < tokens.Count(); i++)
        {
            var (op, acc) = tokens[i];

            var addToPrefix = prefixOp != null;

            var groupOp = stack[stack.Count - 1];
            if (stack.Count > 1 && op.Operator == groupOp.Op.Value.EndOperator.ToString())
            {
                if (addToPrefix)
                    throw new InvalidOperationException("Ungrouping is prefix param");
                stack.RemoveAt(stack.Count - 1);
                continue;
            }

            var c = new TokenGroup
            {
                Op = op,
                Accessor = acc,
                Order = (uint)i,
            };

            if (addToPrefix)
            {
                if (op.Operator == DefaultOp && prefixOp.Accessor == null)
                    prefixOp.Accessor = acc;
                else
                    prefixOp.Children = new List<TokenGroup> { c };
                prefixOp = null;
            }
            else
            {
                groupOp.Children.Add(c);
            }

            switch (c.Op?.Type)
            {
                case OpTokenType.Group:
                    c.Children = new List<TokenGroup>();
                    stack.Add(c);
                    break;
                case OpTokenType.Prefix:
                    if (c.Accessor == null)
                        prefixOp = c;
                    break;
            }
        }

        if (prefixOp != null)
            throw new InvalidOperationException("Prefix lacks param");

        return stack[0].Children;
    }

}