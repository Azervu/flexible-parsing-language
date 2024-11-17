using System;
using System.Collections.Generic;
using System.Linq;
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
        return tokens;
    }
}