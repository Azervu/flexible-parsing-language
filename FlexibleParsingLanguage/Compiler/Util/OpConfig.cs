﻿using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler.Util;

internal class OpConfig
{
    internal string Operator { get; set; }
    internal string GroupOperator { get; set; }

    internal OpCategory Category { get; set; }
    internal int Rank { get; set; }


    internal Func<ParseData, RawOp, IEnumerable<ParseOperation>> Compile { get; set; }

    internal OpConfig(string op, OpCategory type, Func<ParseData, RawOp, IEnumerable<ParseOperation>> compile = null, int rank = -1, string op2 = null)
    {
        Operator = op;
        Category = type;
        Rank = rank;
        GroupOperator = op2;
        Compile = compile;
    }


    internal int PrefixRank()
    {
        if (Category.All(OpCategory.RightInput))
            return Rank;

        return int.MinValue;
    }

    internal int PostfixRank()
    {
        if (Category.All(OpCategory.LeftInput))
            return Rank;
        return int.MinValue;
    }

}

[Flags]
internal enum OpCategory
{
    None      = 0b_0000_0000_0000_0000,
    Default   = 0b_0000_0001_0000_0000,
    Root      = 0b_0000_0010_0000_0000,
    RootParam     = 0b_0000_1000_0000_0000,





    RightInput    = 0b_0000_0000_0000_0001,
    LeftInput   = 0b_0000_0000_0000_0010,
    ParentInput = 0b_0000_0000_0000_0100,
    Branching = 0b_0000_0000_0000_1000, //Prefix and Postfix passes through


    Group        = 0b_0000_0000_0001_0000,
    UnGroup      = 0b_0000_0000_0010_0000,
    GroupSeparator = 0b_0000_0000_0010_0000,

    Virtual = 0b_0000_0000_1000_0000,



    Literal    = 0b_0001_0000_0000_0000,
    Unescape   = 0b_0010_0000_0000_0000,
    Temp       = 0b_0100_0000_0000_0000,
    Accessor   = 0b_1000_0000_0000_0000,

}

internal static class OpCatergoryExtension
{
    internal static bool All(this OpCategory flag, OpCategory value) => (flag & value) == value;
    internal static bool Any(this OpCategory flag, OpCategory value) => (flag & value) > 0;
}