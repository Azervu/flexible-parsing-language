﻿using FlexibleParsingLanguage.Compiler.Util;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler;

internal partial class ParseContext
{
    internal string Operator { get => Token.Op?.Operator; }
    internal bool Numeric { get => Operator == "[" || Operator == "*" || Operator == "@"; }

    private List<ParseContext> _children;
    internal List<ParseContext> ChildOperator {
        get
        {
            if (_children == null)
            {
                if (Token.Children == null)
                    return null;

                _children = Token.Children.Select(x => new ParseContext(x)).ToList();
            }
            return _children;
        }
    }

    internal int Index { get; set; }

    internal TokenGroup Token { get; set; }

    internal ParseContext(TokenGroup token)
    {
        Token = token;
    }

    internal ParseContext NextReadOperator()
    {
        for (var j = Index + 1; j < ChildOperator.Count; j++)
        {
            var op = ChildOperator[j];
            if (op != null)
                return op.FirstRead();

            return op;
        }
        return null;
    }

    internal ParseContext FirstRead()
    {
        var writes = false;

        if (ChildOperator == null)
            return this;

        foreach (var op in ChildOperator)
        {
            if (op != null)
                return op.FirstRead();
            if (writes)
                return op;
            if (op.Operator == ":")
                writes = true;
        }
        return null;
    }
}
