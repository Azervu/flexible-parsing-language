using FlexibleParsingLanguage.Operations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FlexibleParsingLanguage.Compiler;

public partial class FplCompiler
{
    internal class DesugariserData
    {
        private int _index;

        public string GetName() => (++_index).ToString();

        internal DesugariserData(int index)
        {
            _index = index;
        }
    }

    private void Desugarize(ref List<Token> tokens)
    {

        var validSlot = 0;

        var maxIndex = 0;
        foreach (var op in tokens)
        {
            if (int.TryParse(op.Accessor, out var index))
                maxIndex = Math.Max(maxIndex, index);
        }

        var d = new DesugariserData(maxIndex);
        var stack = new Stack<(int, Token, string)>();
        string activeName = string.Empty;
        OpConfig lastOp = null;

        for (var i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];

            if (t?.Op != null && !t.Op.SequenceType.Any(OpSequenceType.Accessor | OpSequenceType.Virtual | OpSequenceType.UnGroup))
                lastOp = t.Op;

            t.FallbackAccessor = activeName;
            if (stack.TryPeek(out var r))
            {
                var (startIndex, startOp, stackActiveName) = r;
                if (t.Op != null && r.Item2.Op.GroupOperator == t.Op.Operator)
                {
                    stack.Pop();
                    activeName = stackActiveName;
                    if (r.Item2.Op.SequenceType.All(OpSequenceType.Group | OpSequenceType.Branching))
                    {
                        tokens.RemoveAt(i);

                        if (lastOp != FplOperation.Write)
                        {
                            tokens.Insert(i, new Token(FplOperation.Write, null, -1));
                            i++;
                        }

                        tokens.InsertRange(i, [
                            new Token(FplOperation.AccessVariable, null, i),
                            new Token(null, activeName, i),
                        ]);
                        i += 2 - 1;
                    }
                }
            }

            if (t.Op == null)
                continue;

            if (t.Op.SequenceType.Any(OpSequenceType.LeftInput))
            {
                validSlot = i;
            }

            if (t.Op == FplOperation.AccessVariable)
            {
                if (i >= tokens.Count - 1 || tokens[i + 1].Op != null)
                {
                    tokens.Insert(i + 1, new Token(null, activeName, i + 1));
                }
                continue;
            }


            if (t.Op.SequenceType.Any(OpSequenceType.Group))
            {
                activeName = d.GetName();

                if (t.Op.SequenceType.Any(OpSequenceType.Branching))
                {
                    tokens.RemoveAt(i);
                    i--;
                }
                tokens.InsertRange(validSlot, [
                    new Token(FplOperation.SetVariable, null, i),
                    new Token(null, activeName, i),
                ]);
                i += 2;

                stack.Push((i, t, activeName));
                continue;
            }
        }

        if (stack.Count > 0)
        {
            var (_, t, n) = stack.Pop();
            throw new QueryException(new RawOp()
            {
                CharIndex = t.Index,
                Type = t.Op
            }, "Unclosed Token");
        }

        if (lastOp != FplOperation.Write)
        {
            tokens.Add(new Token(FplOperation.Write, null, -1));
        }
        RemoveRedunantTokens(ref tokens);

    }


    private void RemoveRedunantTokens(ref List<Token> tokens)
    {
        var redirectRef = new Dictionary<string, string>();

        string activeName = null;

        var i = 0;
        while (i + 1 < tokens.Count)
        {
            var t = tokens[i];
            var acc = tokens[i+1].Accessor;
            if (t.Op != FplOperation.SetVariable || string.IsNullOrEmpty(acc))
            {
                activeName = null;
                i++;
                continue;
            }

            if (activeName == null)
            {
                activeName = acc;
                i += 2;
                continue;
            }

            redirectRef[acc] = activeName;
            tokens.RemoveRange(i, 2);
        }


        i = 0;
        while (i + 1 < tokens.Count)
        {
            var t = tokens[i];
            var acc = tokens[i + 1].Accessor;
            if (t.Op != FplOperation.AccessVariable || string.IsNullOrEmpty(acc))
            {
                activeName = null;
                i++;
                continue;
            }

            if (redirectRef.TryGetValue(acc, out var acc2))
            {
                acc = acc2;
                tokens[i + 1].Accessor = acc2;
            }

            if (acc != activeName)
            {
                activeName = acc;
                i += 2;
                continue;
            }
            tokens.RemoveRange(i, 2);
        }
    }




    private string DesugarizedQuery(List<Token> tokens)
    {
        var lastAccessor = false;
        var query = new StringBuilder();
        foreach (var token in tokens)
        {
            token.Index = query.Length;
            if (token.Op?.SequenceType.Any(OpSequenceType.Literal) == true)
            {
                query.Append($"'{token.Accessor}'");
                lastAccessor = true;
            }
            else
            {
                if (token.Op?.Operator != null)
                {
                    query.Append(token.Op?.Operator);
                }
                else if (lastAccessor)
                {
                    query.Append(DefaultOp.Operator);
                }

                lastAccessor = token.Accessor != null;
                if (lastAccessor)
                    query.Append(token.Accessor);

            }
        }
        return query.ToString();
    }
}