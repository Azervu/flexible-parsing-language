using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FlexibleParsingLanguage.Compiler.Util.Lexicalizer;

namespace FlexibleParsingLanguage.Compiler.Util;


internal class RawOp
{
    internal int Id { get; set; }
    internal int CharIndex { get; set; }
    internal OpConfig Type { get; set; }
    internal string? Accessor { get; set; }


    internal List<RawOp> LeftInput { get; private set; } = new List<RawOp>();
    internal List<RawOp> RightInput { get; private set; } = new List<RawOp>();



    internal IEnumerable<RawOp> GetInput() => LeftInput.Concat(RightInput);


    internal List<RawOp> Output { get; set; } = new List<RawOp>();

    internal bool Prefixed { get; set; }
    internal bool PostFixed { get; set; }


    internal bool IsPrefix()
    {
        if (Type.Category.Has(OpCategory.Prefix) && !Prefixed)
            return true;

        return false;
    }

    internal bool IsPostfix()
    {
        if (Type.Category.Has(OpCategory.Postfix) && !PostFixed)
            return true;

        return false;
    }

    internal bool TryAddPrefix(RawOp op)
    {
        if (!IsPrefix())
            return false;
        Prefixed = true;
        LeftInput.Add(op);
        return true;
    }

    internal bool TryAddPostfix(RawOp op)
    {
        if (!IsPostfix())
            return false;
        PostFixed = true;
        RightInput.Insert(0, op);
        return true;
    }


    internal bool IsBranch() => Type.Category.Has(OpCategory.Branching);

    internal int PrefixRank()
    {
        if (IsPrefix())
            return Type.Rank;
        return int.MinValue;
    }

    internal int PostfixRank()
    {
        if (IsPostfix())
            return Type.Rank;
        return int.MinValue;
    }

    internal List<RawOp> Children = new List<RawOp>();


    internal void AddLog(StringBuilder log, int depth)
    {
        log.Append($"\n{new string(' ', 4 * depth)}");
        if (Type?.Operator != null)
        {
            log.Append(Type?.Operator);

            if (Accessor != null)
                log.Append($"  ");
        }
        if (Accessor != null)
            log.Append($"\"{Accessor}\"");

        if (Children != null)
        {
            foreach (var c in Children)
                c.AddLog(log, depth + 1);
        }
    }

    internal string ToString2()
    {
        var l = new StringBuilder();
        AddLog(l, 0);
        return l.ToString();
    }
}
