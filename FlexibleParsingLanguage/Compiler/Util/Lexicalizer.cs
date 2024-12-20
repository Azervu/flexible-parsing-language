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
                HandleConfigEntry(op2, new OpConfig(op2, OpCategory.Temp));
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

        tokens = tokens.Select(x => {

            if (x.Item1 != UnknownOp)
                return x;
            return (defaultOp, x.Item2);
        }).ToList();


        var ops = new List<TokenGroup>(tokens.Count);
        for (var i = 0; i < tokens.Count; i++)
        {
            var (op, acc) = tokens[i];
            ops.Add(new TokenGroup
            {
                Type = op,
                Accessor = acc,
                Order = (uint)i,
            });
        }

        ops = GroupOps(ops);
        SequenceOps(ops);





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

    private List<TokenGroup> GroupOps(List<TokenGroup> ops)
    {
        var stack = new List<TokenGroup> { new() { Type = new OpConfig(null, OpCategory.Temp), Children = [] } };
        TokenGroup? prefixOp = null;

        foreach (var op in ops)
        {
            var groupOp = stack[stack.Count - 1];
            if (stack.Count > 1 && op.Type.Operator == groupOp.Type.GroupOperator.ToString())
            {
                stack.RemoveAt(stack.Count - 1);
                continue;
            }
            groupOp.Children.Add(op);

            if (op.Type?.GroupOperator != null)
            {
                op.Children = new List<TokenGroup>();
                stack.Add(op);
            }

        }

        if (prefixOp != null)
            throw new InvalidOperationException("Prefix lacks param");
        return stack[0].Children;
    }


    private class SequenceTemp
    {
        internal int LeftIndex { get; set; }
        internal int RightIndex { get; set; }
        internal int Index { get; set; }
        internal List<SequenceTemp> Input { get; set; } = new List<SequenceTemp>();
        internal TokenGroup Op { get; set; }
        internal void AddInput(SequenceTemp seq, List<SequenceTemp> opsTemp)
        {
            if (seq.LeftIndex >= 0 && seq.LeftIndex != Index)
                opsTemp[seq.LeftIndex].RightIndex = Index;

            if (seq.RightIndex >= 0 && seq.RightIndex != Index)
                opsTemp[seq.RightIndex].LeftIndex = Index;

            seq.LeftIndex = -1;
            seq.RightIndex = -1;
            if (Input.Count > 0 && Input[Input.Count - 1] != null)
                seq.LeftIndex = Input[Input.Count - 1].Index;
            Input.Add(seq);
        }
    };

    internal void SequenceOps(List<TokenGroup> ops)
    {
        var opsTemp = new List<SequenceTemp>(ops.Count);
        var leftIndex = -2;
        var awaitingRight = new List<SequenceTemp>();

        for (var i = 0; i < ops.Count; i++)
        {
            var op = ops[i];
            var seq = new SequenceTemp
            {
                Op = op,
                LeftIndex = leftIndex,
                RightIndex = -2,
                Index = i,
            };
            opsTemp.Add(seq);

            if (op.Type.Category.Has(OpCategory.Branch))
            {
                awaitingRight.Add(seq);
            }
            else
            {
                foreach (var ar in awaitingRight)
                    ar.RightIndex = i;
                leftIndex = i;
                awaitingRight = [seq];
            }
        }


        var ordered = opsTemp.Select(x => x).ToList();
        ordered.OrderByDescending(x => x.Op.Type.Rank).ThenBy(x => x.Index);
        foreach (var op in ordered)
        {
            if (op.Op.Type.Category.Has(OpCategory.Prefix))
            {
                if (op.LeftIndex >= 0)
                    op.AddInput(opsTemp[op.LeftIndex], opsTemp);
                else if (op.LeftIndex == -2)
                    op.Input.Add(null);
            }
            if (op.Op.Type.Category.Has(OpCategory.Postfix))
            {
                if (op.RightIndex >= 0)
                    op.AddInput(opsTemp[op.RightIndex], opsTemp);
                else if (op.RightIndex == -2)
                    op.Input.Add(null);
            }
        }


        foreach (var op in opsTemp)
        {
            op.Op.Input = op.Input.Select(x => x?.Op).ToList();
            foreach (var input in op.Input)
            {
                if (input == null)
                    continue;

                input.Op.Output.Add(op.Op);
            }
        }
    }



































    private List<TokenGroup> GroupTokens(List<(OpConfig, string?)> tokens)
    {
        var stack = new List<TokenGroup> { new() { Type = new OpConfig(null, OpCategory.Temp), Children = [] } };
        TokenGroup? prefixOp = null;

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


            if (c.Type.GroupOperator != null)
            {
                c.Children = new List<TokenGroup>();
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
