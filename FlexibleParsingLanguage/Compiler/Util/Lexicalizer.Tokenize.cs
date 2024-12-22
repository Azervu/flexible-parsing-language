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
        internal OpConfig? Op;
        internal string? Accessor;
        internal int Index;

        internal Token(OpConfig op, int i)
        {
            Op = op;
            Index = i;
        }

        internal Token(string accessor, int i)
        {
            Accessor = accessor;
            Index = i;
        }
    }


    private IEnumerable<Token> Tokenize(string raw)
    {









        /*





        int opIndex = -5;
        string accessor = string.Empty;
        int accessorIndex = -5;

        OpConfig? opCandidate = null;
        string candidateString = string.Empty;

        OpConfig? opEscape = null;
        int startIndex = 0;
        OpConfig? op = null;
        */

        /*


   
        OpConfig? opCandidate = null;
        int candidateIndex = 0;

        OpConfig? op = null;
        int opIndex = -1;
        */

        var searchedIndex = 0;
        while (searchedIndex < raw.Length-1)
        {
            //var accessor = string.Empty;
            //var accessorIndex = -1;

            var i = searchedIndex;
            var candidate = string.Empty;
            var candidateIndex = i;

            OpConfig? op = null;
            int opStart = -1;
            int opEnd = -1;

            string accessor = string.Empty;
            int accessorIndex = -1;
            while (i < raw.Length - 1)
            {
                var c = raw[i].ToString();
                candidate += c;
                i++;


                if (!Operators.TryGetValue(candidate, out var op2))
                {
                    if (op != null)
                        break;
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
                        yield return new Token(accessor, accessorIndex);
                        accessor = string.Empty;
                        accessorIndex = -1;
                        searchedIndex = candidateIndex;
                        break;
                    }
                }
                    /*

                    if (!Operators.TryGetValue(candidate, out var op6))
                    {
                        if (accessorIndex == -1)
                            accessorIndex = candidateIndex;

                        accessor += candidate;
                        candidate = string.Empty;
                        candidateIndex = i;

                        if (opCandidate != null)
                        {
                            op = opCandidate;
                            opCandidate = null;
                            yield return new Token
                            {
                                Op = opCandidate,
                                Index = 

                            };
                        }
                    }
                    else if (op6 != null)
                    {
                        opCandidate = op6;
                    }
                    */
            }


            if (op == null)
                continue;


            if (!op.Category.Has(OpCategory.Literal))
            {
                yield return new Token(op, opStart);
                searchedIndex = opEnd;
                continue;
            }
            i = opEnd;

            var escapedString = string.Empty;
            var escaped = false;
            while (i < raw.Length - 1)
            {
                var c = raw[i].ToString();
                escapedString += c;

                if (escapedString.EndsWith(UnescapeToken))
                {
                    escapedString = escapedString.Substring(0, escapedString.Length - UnescapeToken.Length);
                    i++;
                    continue;
                }

                if (escapedString.EndsWith(op.GroupOperator))
                {
                    escapedString = escapedString.Substring(0, escapedString.Length - op.GroupOperator.Length);
                    escaped = true;
                    break;
                }
                i++;
            }

            if (!escaped)
                throw new QueryCompileException(new RawOp
                {
                    Type = op,
                    CharIndex = raw.Length - 1
                }, "Escape operator not ended");
            searchedIndex = i + 1;

            yield return new Token(escapedString, searchedIndex);









            /*



            if (!Operators.TryGetValue(candidate, out var op2))
            {
                if (!Operators.TryGetValue(c, out op2))
                {
                    if (opCandidate != null)
                    {
                        op = opCandidate;
                        opIndex = candidateIndex;
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
                            accessorIndex = searchedIndex;
                        accessor += c;
                    }
                    continue;
                }

                candidate = c;

                if (op2 != null)
                {

                }



            }
            */



            /*
            if (!Operators.TryGetValue(candidate, out var op2))
            {
                if (opCandidate != null)
                {
                    op = opCandidate;
                    opIndex = candidateIndex;
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
                    yield return new Token { Op = op ?? DefaultOp, OpIndex = (op == null ? accessorIndex : opIndex), Accessor = accessor, AccessorIndex = accessorIndex };
                    candidateIndex = i;
                    op = null;
                    opIndex = -2;
                    accessor = string.Empty;
                    accessorIndex = -2;
                }

                if (op2.Category.Has(OpCategory.Literal))
                {
                    if (op == null)
                        candidateIndex = i;

                    string opEscapeString = string.Empty;
                    while (i < raw.Length - 1)
                    {
                        i++;
                        c = raw[i].ToString();
                        opEscapeString += c;

                        if (!opEscapeString.EndsWith(opEscape.GroupOperator))
                            continue;
                        
                        var escapeStartIndex = i - opEscapeString.Length;
                        opEscapeString = opEscapeString.Substring(0, opEscapeString.Length - opEscape.GroupOperator.Length);
                        yield return new Token { Op = op ?? DefaultOp, OpIndex = (op == null ? escapeStartIndex : opIndex), Accessor = opEscapeString, AccessorIndex = escapeStartIndex };
                        opEscape = null;
                        accessor = string.Empty;
                        accessorIndex = -4;
                        op = null;
                        opIndex = -4;
                        candidateIndex = i + 1;
                        break;
                    }

                    if (i == raw.Length)
                    {
                        throw new QueryCompileException(new RawOp
                        {
                            Type = opEscape,
                            CharIndex = raw.Length - 1
                        }, "Escape operator not ended");
                    }


                }
                else
                {
                    if (op != null)
                    {
                        yield return new Token { Op = op, OpIndex = opIndex, Accessor = accessor, AccessorIndex = accessorIndex };
                        op = null;
                        opIndex = -3;
                        accessor = string.Empty;
                        accessorIndex = -3;
                        candidateIndex = i;
                    }
                    opCandidate = op2;
                }
            }
            */
        
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



    private void FindAccessorToken(ref int i, string raw, out string preAccessor, out OpConfig? op, out string postAccessor)
    {
        op = null;
        var startIndex = i;
        preAccessor = string.Empty;
        postAccessor = string.Empty;






        var candidate = raw[i].ToString();
        OpConfig? opCandidate = null;








    }


}

