using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler.Util;

internal class OpConfig
{
    internal string Operator { get; set; }
    internal char? GroupOperator { get; set; }
    internal bool Branching { get; set; }


    internal OpCategory Category { get; set; }
    internal int Rank { get; set; }
    internal OpConfig(string op, OpCategory type, int rank = -1, char? endOperator = null)
    {
        Operator = op;
        Category = type;
        Rank = rank;
        GroupOperator = endOperator;
    }

    internal int PrefixRank()
    {
        switch (Category)
        {
            case OpCategory.Prefix:
            case OpCategory.Infix:
                return Rank;
        }
        return int.MinValue;
    }

    internal int PostfixRank()
    {
        switch (Category)
        {
            case OpCategory.Prefix:
            case OpCategory.Infix:
                return Rank;
        }
        return int.MinValue;
    }

}

internal enum OpCategory
{
    Any,



    Temp,
    Unknown,

    Prefix,
    PostFix,
    Infix,
    Literal,
    Singleton,

}