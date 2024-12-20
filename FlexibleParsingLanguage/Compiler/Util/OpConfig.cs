using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler.Util;

internal struct OpConfig
{
    internal string Operator { get; set; }
    internal char? EndOperator { get; set; }
    internal OpTokenType Type { get; set; }
    internal OpConfig(string op, OpTokenType type, char? endOperator = null)
    {
        Operator = op;
        EndOperator = endOperator;
        Type = type;
    }
}

internal enum OpTokenType
{
    Prefix,
    Escape,
    Group,
    Singleton,
    Temp,
}