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

    internal bool Prefixed { get; set; }
    internal bool PostFixed { get; set; }





    internal int CharIndex { get; set; }

    internal int Id { get; set; }







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
    internal List<OpConfig> Ops { get; private set; }

    private OpConfig DefaultOp { get; set; }
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

            if (op.Category.Has(OpCategory.Unescape))
                UnescapeToken = op.Operator[0];
        }
        if (DefaultOp == null)
            throw new Exception("Default operator missing");

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

        var ops = new List<RawOp>(tokens.Count);
        var idCounter = 1;

        foreach (var (op, acc, charIndex) in tokens)
        {
            if (op == DefaultOp && string.IsNullOrEmpty(acc))
                continue;

            var rawOp = new RawOp
            {
                Id = idCounter++,
                CharIndex = charIndex,
                Type = op,
            };


            ops.Add(rawOp);
            if (!string.IsNullOrEmpty(acc))
            {
                if (op.Category.Has(OpCategory.Prefix))
                {
                    var accessOp = new RawOp
                    {
                        Id = idCounter++,
                        CharIndex = charIndex,
                        Type = AccessorOp,
                        Accessor = acc,
                    };
                    rawOp.Prefixed = true;
                    rawOp.Input.Add(accessOp);
                }
                else
                {
                    var defaultOp = new RawOp
                    {
                        Id = idCounter++,
                        CharIndex = charIndex,
                        Type = DefaultOp,
                        Accessor = acc,
                    };
                    ops.Add(defaultOp);
           
                }
            }
     
        }


        for (var i = 0; i < tokens.Count; i++)
        {
            var (op, acc, charIndex) = tokens[i];




        }
        for (var i = 0; i < ops.Count; i++)
        {
            var op = ops[i];
        }


        Sequence(ref ops);







        tokens = tokens.Select(x => {

            if (x.Item1 != AccessorOp)
                return x;
            return (DefaultOp, x.Item2, x.Item3);
        }).ToList();


        return (GroupTokens(tokens), ops);
    }


    private IEnumerable<(OpConfig, string?, int)> Tokenize(string raw)
    {

        OpConfig? op = null;
        string accessor = string.Empty;

        OpConfig? opCandidate = null;
        string candidateString = string.Empty;

        OpConfig? opEscape = null;
        string opEscapeString = string.Empty;

        int startIndex = -1;

        for (var i = 0; i < raw.Length; i++)
        {
            var c = raw[i];
            if (opEscape != null)
            {
                if (opEscapeString.EndsWith(UnescapeToken))
                    continue;

                opEscapeString += c;
                if (opEscapeString.EndsWith(opEscape.GroupOperator))
                {
                    opEscapeString = opEscapeString.Substring(0, opEscapeString.Length - opEscape.GroupOperator.Length);
                    yield return (op ?? DefaultOp, accessor, startIndex);
                    opEscape = null;
                    accessor = string.Empty;
                    op = null;
                    startIndex = i + 1;
                }
                continue;
            }

            candidateString += c;

            //var nextCandidate = candidateString + c;
            if (!Operators.TryGetValue(candidateString, out var op2))
            {
                if (opCandidate != null)
                {
                    op = opCandidate;
                }
                accessor += c;
                candidateString = string.Empty;
            }
            else if (op2 != null)
            {
                if (accessor != string.Empty)
                {
                    yield return (op ?? DefaultOp, accessor, startIndex);
                    startIndex = i;
                    op = null;
                    accessor = string.Empty;
                }

                if (op2.Category.Has(OpCategory.Literal))
                {
                    if (op == null)
                        startIndex = i;

                    opEscape = op2;
                }
                else
                {
                    if (op != null)
                    {
                        yield return (op, accessor, startIndex);
                        startIndex = i;
                        op = null;
                        accessor = string.Empty;
                    }
                    opCandidate = op2;
                }
            }
        }








    }

    private List<RawOp> GroupTokens(List<(OpConfig, string?, int)> tokens)
    {
        var stack = new List<RawOp> { new() { Type = new OpConfig(null, OpCategory.Temp), Children = [] } };
        RawOp? prefixOp = null;

        for (var i = 0; i < tokens.Count(); i++)
        {
            var (op, acc, charIndex) = tokens[i];

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