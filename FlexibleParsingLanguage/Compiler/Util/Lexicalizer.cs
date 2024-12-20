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
    internal OpConfig? Type { get; set; }
    internal string? Accessor { get; set; }

    internal List<TokenGroup> Input { get; set; } = new List<TokenGroup>();
    internal List<TokenGroup> Output { get; set; } = new List<TokenGroup>();




    internal uint Order { get; set; }



    internal List<TokenGroup>? Children { get; set; }


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

internal class Lexicalizer
{
    private string DefaultOp { get; set; }
    private char UnescapeToken { get; set; }

    private OpConfig UnknownOp { get; set; } = new OpConfig(null, OpTokenType.Unknown);

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


        var defaultOp = Operators[DefaultOp] ?? throw new Exception("Default operator missing");
        var t2 = tokens.Select(x => {
        
            if (x.Item1 != UnknownOp)
                return x;
            return (defaultOp, x.Item2);
        }).ToList();


        var groupedTokens = GroupTokens(t2);
        return groupedTokens;
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

                if (op.Type == OpTokenType.Literal)
                {
                    active = string.Empty;
                    while (true)
                    {
                        if (it.Current == op.EndOperator)
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

    private List<TokenGroup> GroupTokens(List<(OpConfig, string?)> tokens)
    {
        var stack = new List<TokenGroup> { new() { Type = new OpConfig(null, OpTokenType.Temp), Children = [] } };
        TokenGroup? prefixOp = null;

        for (var i = 0; i < tokens.Count(); i++)
        {
            var (op, acc) = tokens[i];

            var addToPrefix = prefixOp != null;

            var groupOp = stack[stack.Count - 1];
            if (stack.Count > 1 && op.Operator == groupOp.Type.EndOperator.ToString())
            {
                if (addToPrefix)
                    throw new InvalidOperationException("Ungrouping is prefix param");
                stack.RemoveAt(stack.Count - 1);
                continue;
            }

            var c = new TokenGroup
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
                    prefixOp.Children = new List<TokenGroup> { c };
                prefixOp = null;
            }
            else
            {
                groupOp.Children.Add(c);
            }

            switch (c.Type?.Type)
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



    internal void HandlePostPreFix(List<(OpConfig, string?)> tokens)
    {

    }


    internal void SequenceTokens(List<(OpConfig, string?)> tokens)
    {




        var tokenGroups = new List<TokenGroup>();


        var rootToken = new TokenGroup();


        for (var i = 0; i < tokens.Count(); i++)
        {
            var (op, acc) = tokens[i];

            var input = new List<(OpConfig, string?)>();
            var output = new List<(OpConfig, string?)>();


            switch (op.Type)
            {
                case OpTokenType.Prefix:
                case OpTokenType.Infix:



                    break;
            }



            switch (op.Type)
            {
                case OpTokenType.Prefix:
                    break;
                case OpTokenType.PostFix:
                    break;
                case OpTokenType.Infix:
                    break;
                case OpTokenType.Literal:
                    break;
                case OpTokenType.Group:
                    break;
                case OpTokenType.Singleton:
                    break;
                case OpTokenType.Temp:
                    break;
            }


        }

    }

}
