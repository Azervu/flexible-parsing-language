using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace FlexibleParsingLanguage.Compiler;

internal partial class Compiler
{
    private struct Token
    {
        internal OpConfig? Op;
        internal string? Accessor;
        internal int Index;

        internal Token(OpConfig op, int i)
        {
            Op = op;
            Index = i;
        }

        internal Token(OpConfig? op, string accessor, int i)
        {
            Op = op;
            Accessor = accessor;
            Index = i;
        }
    }

    private IEnumerable<Token> Tokenize(string raw)
    {
        var searchedIndex = 0;

        while (searchedIndex < raw.Length)
        {
            var candidate = string.Empty;

            var i = searchedIndex;

            var candidateIndex = i;

            OpConfig? op = null;
            int opStart = -1;
            int opEnd = -1;

            string accessor = string.Empty;
            int accessorIndex = -1;
            while (i < raw.Length)
            {
                var c = raw[i].ToString();
                candidate += c;
                i++;


                if (!Operators.TryGetValue(candidate, out var op2))
                {
                    if (op != null)
                        break;

                    searchedIndex = i;
                    accessor += candidate;
                    if (accessorIndex < 0)
                        accessorIndex = candidateIndex;

                    candidate = string.Empty;
                    candidateIndex = i;
                }
                else if (op2 != null)
                {
                    op = op2;
                    opStart = candidateIndex;
                    opEnd = i;

                    if (accessor != string.Empty)
                    {
                        yield return new Token(null, accessor, accessorIndex);
                        accessor = string.Empty;
                        accessorIndex = -1;
                        searchedIndex = candidateIndex;
                    }
                }
            }

            if (accessor != string.Empty)
                yield return new Token(null, accessor, accessorIndex);

            if (op == null)
                continue;


            if (!op.SequenceType.All(OpSequenceType.Literal))
            {
                yield return new Token(op, opStart);
                searchedIndex = opEnd;
                continue;
            }
            i = opEnd;

            var escapedString = string.Empty;
            var checkString = string.Empty;
            var escaped = false;
            var unescaped = false;
            opStart = i - 1;
            for (; i < raw.Length; i++)
            {
                var c = raw[i].ToString();

                escapedString += c;

                if (unescaped)
                {
                    unescaped = false;
                    checkString = string.Empty;
                    continue;
                }

                if (escapedString.EndsWith(UnescapeToken))
                {
                    unescaped = true;
                    escapedString = escapedString.Substring(0, escapedString.Length - UnescapeToken.Length);
                    continue;
                }

                checkString += c;

                if (checkString.EndsWith(op.GroupOperator))
                {
                    escapedString = escapedString.Substring(0, escapedString.Length - op.GroupOperator.Length);
                    escaped = true;
                    break;
                }
            }

            if (!escaped)
                throw new QueryException(new RawOp
                {
                    Type = op,
                    CharIndex = raw.Length - 1
                }, "Escape operator not ended");
            searchedIndex = i + 1;

            yield return new Token(op, escapedString, opStart);
        }
    }
}
