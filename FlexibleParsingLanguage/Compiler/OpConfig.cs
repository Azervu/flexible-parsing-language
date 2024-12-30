﻿using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler;

internal class OpConfig
{



    internal string Operator { get; set; }
    internal string GroupOperator { get; set; }

    internal OpSequenceType SequenceType { get; set; }
    internal OpCompileType CompileType { get; set; }
    internal int Rank { get; set; }

    internal Func<ParseData, RawOp, int> GetStatusId { get; set; }
    internal Func<ParseData, RawOp, IEnumerable<ParseOperation>> Compile { get; set; }


    



    internal OpConfig(string op, OpSequenceType sequenceType, OpCompileType compileType, Func<ParseData, RawOp, IEnumerable<ParseOperation>> compile = null, int rank = -1, string op2 = null)
    {
        Operator = op;
        SequenceType = sequenceType;
        CompileType = compileType;
        Rank = rank;
        GroupOperator = op2;
        Compile = compile;
    }

    internal OpConfig(string op, OpSequenceType type, Func<ParseData, RawOp, IEnumerable<ParseOperation>> compile = null, int rank = -1, string op2 = null)
    {
        Operator = op;
        SequenceType = type;
        Rank = rank;
        GroupOperator = op2;
        Compile = compile;
    }


    internal int PrefixRank()
    {
        if (SequenceType.All(OpSequenceType.RightInput))
            return Rank;

        return int.MinValue;
    }

    internal int PostfixRank()
    {
        if (SequenceType.All(OpSequenceType.LeftInput))
            return Rank;
        return int.MinValue;
    }

}

[Flags]
internal enum OpSequenceType
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

[Flags]
internal enum OpCompileType
{
    None = 0,
    WriteObject = 1,
    WriteArray = 2,
}

internal static class OpCatergoryExtension
{
    internal static bool All(this OpSequenceType flag, OpSequenceType value) => (flag & value) == value;
    internal static bool Any(this OpSequenceType flag, OpSequenceType value) => (flag & value) > 0;
}