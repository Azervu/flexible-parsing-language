using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler;

internal class Token
{
    internal OpConfig? Op;
    internal string? Accessor;
    internal string? FallbackAccessor;
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