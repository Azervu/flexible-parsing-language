using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler;

internal class Tokenizer
{
    private char DefaultOp { get; set; }
    private char UnescapeToken { get; set; }

    private HashSet<char> AccessorToken { get;set; }
    private HashSet<char> TerminatorTokens { get; set; }
    private HashSet<char> SingularTokens { get; set; }
    private HashSet<char> EscapeTokens { get; set; }

    public Tokenizer(
        string operators,
        string singularOperators,
        char defaultOperator,
        string escapeTokens, char unescapeToken
    )
    {
        DefaultOp = defaultOperator;
        UnescapeToken = unescapeToken;
        EscapeTokens = escapeTokens.ToHashSet();
        SingularTokens = singularOperators.ToHashSet();
        AccessorToken = operators.ToHashSet();


        TerminatorTokens = operators.ToHashSet();
       
        TerminatorTokens.Add(DefaultOp);


        foreach (var c in SingularTokens)
            TerminatorTokens.Add(c);

        foreach (var c in EscapeTokens)
            TerminatorTokens.Add(c);
    }



    /*
     
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

     */

    internal List<(char, string?)> Tokenize(string raw)
    {
        var tokens = new List<(char, string?)>();
        foreach (var (c, acc) in SplitString(raw))
        {
            if (SingularTokens.Contains(c))
            {
                tokens.Add((c, string.Empty));

                if (acc.Length > 0)
                    tokens.Add((DefaultOp, acc));
            }
            else
            {
                tokens.Add((c, acc));
            }
        }
        return tokens;
    }


    private IEnumerable<(char, string)> SplitString(string raw)
    {
        bool unescape = false;
        var active = DefaultOp;
        var accessor = new List<char>();

        foreach (var c in raw)
        {
            if (EscapeTokens.Contains(active))
            {
                if (!unescape)
                {
                    if (c == active)
                    {
                        yield return (c, string.Concat(accessor));
                        active = DefaultOp;
                        accessor.Clear();
                        continue;
                    }

                    if (c == UnescapeToken)
                    {
                        unescape = true;
                        continue;
                    }
                }

                unescape = false;
                accessor.Add(c);
                continue;
            }

            if (!TerminatorTokens.Contains(c))
            {
                accessor.Add(c);
                continue;
            }

            if (accessor.Count() > 0 || active != DefaultOp)
                yield return (active, string.Concat(accessor));
            accessor.Clear();
            active = c;
        }

        if (accessor.Count() > 0 || active != DefaultOp)
            yield return (active, string.Concat(accessor));
    }

}