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




    internal OpCategory Category { get; set; }
    internal int Rank { get; set; }
    internal OpConfig(string op, OpCategory type, int rank = -1, char? op2 = null)
    {
        Operator = op;
        Category = type;
        Rank = rank;
        GroupOperator = op2;
    }

    internal int PrefixRank()
    {
        if (Category.Has(OpCategory.Prefix))
            return Rank;

        return int.MinValue;
    }

    internal int PostfixRank()
    {
        if (Category.Has(OpCategory.Postfix))
            return Rank;
        return int.MinValue;
    }

}

[Flags]
internal enum OpCategory
{
    None    = 0b_0000_0000_0000_0000,
    Prefix  = 0b_0000_0000_0000_0001,
    Postfix = 0b_0000_0000_0000_0010,

    Group   = 0b_0000_0000_0000_0100,
    Escape  = 0b_0000_0000_0000_1000,

    Branch  = 0b_0000_0000_0001_0000,


    Temp,
    Unknown,

    Literal,


}

internal static class OpCatergoryExtension
{
    internal static bool Has(this OpCategory flag, OpCategory value) => (flag & value) == value;
}