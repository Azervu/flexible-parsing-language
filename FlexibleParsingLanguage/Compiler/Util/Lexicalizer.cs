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
        SequenceOps(ref ops);





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

                if (op.Category == OpCategory.Literal)
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





    internal void SequenceOps(ref List<TokenGroup> ops)
    {
        var proccessedOps = new HashSet<TokenGroup>();

        var left = new Dictionary<int, int>();
        var right = new Dictionary<int, int>();

        var ranks = new List<(int Rank, int Index)>(ops.Count);







        for (var i = 0; i < ops.Count; i++)
        {
            if (!left.TryGetValue(i, out var l))
                l = i - 1;
            if (!right.TryGetValue(i, out var r))
                r = i > ops.Count - 2 ? -1 : i + 1;
        }





        ranks.OrderByDescending(x => x.Rank).ThenBy(x => x.Index);


        foreach (var (rank, i) in ranks)
        {
            var op = ops[i];

            if (!left.TryGetValue(i, out var l))
                l = i - 1;

            if (!right.TryGetValue(i, out var r))
                r = i > ops.Count - 2 ? -1 : i + 1;




            switch (op.Type.Category)
            {
                case OpCategory.Prefix:
                case OpCategory.Infix:

                    TokenGroup param = null;
                    if (l >= 0)
                    {
                        param = ops[l];

                        //if (left.ContainsKey())

                        left[l] = -1;
                    }
                    op.Input.Add(param);
                    break;
            }
        }


        for (var i = 0; i < ops.Count; i++)
        {


        }




        while (true) {
        
            var minRank = int.MaxValue;

            foreach (var op in ops)
            {
                if (proccessedOps.Contains(op))
                    continue;

                minRank = Math.Min(minRank, op.Type.Rank);
            }

            if (minRank == int.MaxValue)
                break;

            for (var i = 0; i < ops.Count(); i++)
            {
                var op = ops[i];

                if (op.Type.Rank > minRank)
                    continue;











                var postfixRank = op.Type.PostfixRank();
                var prefixRank = op.Type.PrefixRank();

                if (i > 0)
                {
                    var last = ops[i - 1];
                    var lastRank = last.Type.PostfixRank();
                    if (prefixRank != int.MinValue || lastRank != int.MinValue)
                    {
                        if (prefixRank > lastRank)
                        {

                        }
                        else
                        {

                        }
                    }



                }




                proccessedOps.Add(op);

                switch (op.Type.Category)
                {
                    case OpCategory.Prefix:
                    case OpCategory.Infix:

                        TokenGroup param = null;
                        if (i > 0)
                        {
                            param = ops[i - 1];
                            ops.RemoveAt(i - 1);
                            i--;
                        }
                        op.Input.Add(param);
                    break;
                }
                switch (op.Type.Category)
                {
                    case OpCategory.PostFix:
                    case OpCategory.Infix:

                        TokenGroup param = null;
                        if (i < ops.Count()-1)
                        {
                            param = ops[i + 1];
                            ops.RemoveAt(i + 1);
                            i--;
                        }
                        op.Input.Add(param);
                        break;
                }
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

            switch (c.Type?.Category)
            {
                case OpCategory.Prefix:
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
