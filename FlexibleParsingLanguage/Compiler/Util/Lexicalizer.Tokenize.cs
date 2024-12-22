using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler.Util;

internal partial class Lexicalizer
{

    private struct Token
    {
        internal OpConfig Op;
        internal int OpIndex;
        internal string? Accessor;
        internal int AccessorIndex;
    }


    private IEnumerable<Token> Tokenize(string raw)
    {
        OpConfig? op = null;
        int opIndex = -5;
        string accessor = string.Empty;
        int accessorIndex = -5;

        OpConfig? opCandidate = null;
        string candidateString = string.Empty;

        OpConfig? opEscape = null;
        string opEscapeString = string.Empty;

        int startIndex = 0;

        var index = 0;  
        while (index < raw.Length) {
            var c = raw[index].ToString();

            var ii = index;


            yield return new Token { Op = op, OpIndex = opIndex, Accessor = c, AccessorIndex = ii };




        }





        /*
        for (var i = 0; i < raw.Length; i++)
        {
            var c = raw[i].ToString();
            if (opEscape != null)
            {
                if (opEscapeString.EndsWith(UnescapeToken))
                    continue;

                opEscapeString += c;
                if (opEscapeString.EndsWith(opEscape.GroupOperator))
                {
                    var escapeStartIndex = i - opEscapeString.Length;
                    opEscapeString = opEscapeString.Substring(0, opEscapeString.Length - opEscape.GroupOperator.Length);
                    yield return (op ?? DefaultOp, (op == null ? escapeStartIndex : opIndex), opEscapeString, escapeStartIndex);
                    opEscape = null;
                    accessor = string.Empty;
                    accessorIndex = -4;
                    op = null;
                    opIndex = -4;
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
                    opIndex = startIndex;
                }

                if (!Operators.TryGetValue(c, out op2))
                {
                    if (op2 != null)
                    {

                        opCandidate = op2;
                    }
                }
                else
                {
                    if (accessor == string.Empty)
                        accessorIndex = i;
                    accessor += c;
                }
            }
            else if (op2 != null)
            {
                if (accessor != string.Empty)
                {
                    yield return (op ?? DefaultOp, (op == null ? accessorIndex : opIndex), accessor, accessorIndex);
                    startIndex = i;
                    op = null;
                    opIndex = -2;
                    accessor = string.Empty;
                    accessorIndex = -2;
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
                        yield return (op, opIndex, accessor, accessorIndex);
                        op = null;
                        opIndex = -3;
                        accessor = string.Empty;
                        accessorIndex = -3;
                        startIndex = i;
                    }
                    opCandidate = op2;
                }
            }
        }

        if (opEscape != null)
            throw new QueryCompileException(new RawOp
            {
                Type = opEscape,
                CharIndex = raw.Length - 1
            }, "Escape operator not ended");

        if (op != null || accessor != string.Empty)
            yield return (op ?? DefaultOp, opIndex, accessor, accessorIndex);
        */
    }



    private void TryFindAccessorToken(ref int i)
    {

    }


}

