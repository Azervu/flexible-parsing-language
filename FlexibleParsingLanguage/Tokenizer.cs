using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage;

internal class Tokenizer
{

    private char DefaultOp { get; set; }
    private char UnescapeToken { get; set; }
    private HashSet<char> TerminatorTokens { get; set; }
    private HashSet<char> EscapeTokens { get; set; }


    public Tokenizer(string operators, char defaultOperator, string escapeTokens, char unescapeToken)
    {
        DefaultOp = defaultOperator;
        UnescapeToken = unescapeToken;

        EscapeTokens = escapeTokens.ToHashSet();

        TerminatorTokens = operators.ToHashSet();
        TerminatorTokens.Add(defaultOperator);

        foreach (var c in EscapeTokens)
            TerminatorTokens.Add(c);
    }

    internal List<(char, string?)> Tokenize(string raw)
    {
        var tokens = new List<(char, string?)>();
        var completedIndex = 0;

        for (int i = 0; i < raw.Length; i++)
        {
            var c = raw[i];

            if (!TerminatorTokens.Contains(c))
                continue;

            if (i > completedIndex)
            {
                var ac = raw.Substring(completedIndex, i - completedIndex);

                tokens.Add((DefaultOp, ac));
                completedIndex = i;
            }

            if (EscapeTokens.Contains(c))
            {
                i++;
                var escapedStringArray = new List<char>();
                for (; i < raw.Length; i++)
                {


                    if (raw[i] == c)
                        break;
                    if (raw[i] == UnescapeToken)
                        i++;
                    escapedStringArray.Add(raw[i]);
                }
                var escapedString = new string(escapedStringArray.ToArray());
                completedIndex = i + 1;
                tokens.Add((c, escapedString));
            }
            else if (c != DefaultOp)
            {
                tokens.Add((c, null));
                completedIndex = i + 1;
            }
            else
            {
                completedIndex = i + 1;
            }

        }

        if (completedIndex < raw.Length)
        {
            tokens.Add((DefaultOp, raw.Substring(completedIndex, raw.Length - completedIndex)));
        }
        ReOrder(tokens);
        return tokens;
    }

    internal class SorterStack
    {
        internal int Start { get; set; }
        internal int Middle { get; set; }
        internal int End { get; set; }

        internal void Swap<T>(List<T> list)
        {

            if (Middle < 0)
                return;



            var startRange = list.GetRange(Start, Middle - Start);
            var endRange = list.GetRange(Middle + 1, End - Middle);


            var newMiddle = Start + endRange.Count;

            list[newMiddle] = list[Middle];

            for (var j = Start; j < newMiddle; j++)
                list[j] = endRange[j - Start];

            for (var j = newMiddle + 1; j <= End; j++)
                list[j] = startRange[j - newMiddle - 1];
        }
    }

    private void ReOrder(List<(char, string?)> tokens)
    {
        var stack = new List<SorterStack> { new SorterStack { Start = 0, Middle = -1, End = tokens.Count - 1 } };
        for (var i = 0; i < tokens.Count; i++)
        {
            var (token, accessor) = tokens[i];
            var entry = stack.Last();
            switch (token)
            {
                case '{':
                    stack.Add(new SorterStack { Start = i + 1, Middle = -1, End = -1 });
                    break;
                case '}':
                    entry.End = i - 1;
                    stack.Pop().Swap(tokens);
                    break;
                case ':':
                    entry.Middle = i;
                    break;
            }
        }
        stack.Pop().Swap(tokens);
    }

}