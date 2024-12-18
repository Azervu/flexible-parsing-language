using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler;


internal class TokenGroup
{
    internal OpTokenConfig? Op { get; set; }
    internal string Acc { get; set; }
    internal List<TokenGroup> Children { get; set; }
    internal void AddLog(StringBuilder log, int depth)
    {
        log.Append($"\n{new string(' ', 4 * depth)}");


        if (Op?.Operator != null)
        {
            log.Append(Op?.Operator);

            if (Acc != null)
                log.Append($"  ");
        }
        if (Acc != null)
            log.Append($"\"{Acc}\"");

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
        l.Append("\n");
        return l.ToString();
    }
}


internal struct OpTokenConfig
{
    internal string Operator { get; set; }
    internal char? EndOperator { get; set; }
    internal OpTokenType Type { get; set; }
    internal OpTokenConfig(string op, OpTokenType type, char? endOperator = null)
    {
        Operator = op;
        EndOperator = endOperator;
        Type = type;
    }
}

internal enum OpTokenType
{
    Prefix,
    Escape,
    Group,
    Singleton,

}

internal class Lexicalizer
{
    private string DefaultOp { get; set; }
    private char UnescapeToken { get; set; }

    private Dictionary<string, OpTokenConfig?> Operators = new();

    public Lexicalizer(
        List<OpTokenConfig> ops,
        string defaultOperator,
        char unescapeToken
    )
    {

        foreach (var op in ops)
        {
            OpTokenConfig? cop;
            for (var i = 0; i < op.Operator.Length - 1; i++)
            {
                var o = op.Operator.Substring(i, i + 1);
                if (!Operators.TryGetValue(o, out cop))
                    Operators[o] = null;
            }
            Operators[op.Operator] = op;

            if (Operators.TryGetValue(op.Operator, out cop))
                Operators.Remove(op.Operator);
            Operators[op.Operator] = op;
        }

        DefaultOp = defaultOperator;
        UnescapeToken = unescapeToken;
    }

    internal TokenGroup Lexicalize(string raw)
    {

        var stack = new List<TokenGroup> { new TokenGroup { Op = new OpTokenConfig { Operator = "¤" }, Children = new List<TokenGroup>() } };
        TokenGroup? prefixOp = null;

        foreach (var (op, acc) in Tokenize(raw))
        {

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
                Acc = acc
            };


            if (addToPrefix)
            {
                if (op.Operator == DefaultOp && prefixOp.Acc == null)
                    prefixOp.Acc = acc;
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
                    if (c.Acc == null)
                        prefixOp = c;
                    break;
            }
        }

        if (prefixOp != null)
            throw new InvalidOperationException("Prefix lacks param");

        return stack[0];
    }








    private IEnumerable<(OpTokenConfig, string?)> Tokenize(string raw)
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
                    yield return ((OpTokenConfig)op, active);
                }
                else if (op.Value.Operator != DefaultOp)
                {
                    yield return ((OpTokenConfig)op, null);
                }
            }
        }
    }
}