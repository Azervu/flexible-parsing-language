using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler;

internal class ParseContext
{

    internal string Operator { get => Token.Op?.Operator; }
    internal string? Accessor { get => Token.Acc; }
    internal bool Numeric { get => Operator == "[" || Operator == "*" || Operator == "@"; }

    internal ParseContext CurrentChild { get => Accessors[Index]; }
    internal ParseContext NextChild { get => Index + 1 < Accessors.Count ? Accessors[Index + 1] : null; }


    internal bool LastWriteOp { get => (WriteMode == WriteMode.Write || WriteMode == WriteMode.Written) && (NextChild == null || NextChild.Operator == "}"); }

    private List<ParseContext> _children;
    internal List<ParseContext> Accessors {
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

    internal int ActiveId { get; set; }
    internal WriteMode WriteMode { get; set; } = WriteMode.Read;

    internal bool ProcessedEnd { get; set; }

    internal int Index { get; set; }

    internal TokenGroup Token { get; set; }

    internal ParseContext(TokenGroup token)
    {
        Token = token;
    }

    internal ParseContext NextReadOperator()
    {
        for (var j = Index + 1; j < Accessors.Count; j++)
        {
            var op = Accessors[j];
            if (op != null)
                return op.FirstRead();

            return op;
        }
        return null;
    }

    internal ParseContext FirstRead()
    {
        var writes = false;

        if (Accessors == null)
            return this;

        foreach (var op in Accessors)
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
