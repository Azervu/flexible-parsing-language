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


    private List<RawOp> _leftInput = new List<RawOp>();
    private List<RawOp> _rightInput = new List<RawOp>();


    private List<RawOp>? _input = null; 
    
    private List<RawOp> Input {
        get {
            if (_input == null)
                _input = [.. _leftInput, .. _rightInput];
            return _input;
        }
    }
    

    internal IEnumerable<RawOp> GetInput() => Input.AsEnumerable();


    internal void AddPostfix(RawOp op)
    {
        _input = null;
        _leftInput.Add(op);
    }

    internal void AddChildInput(RawOp op)
    {
        Children.Add(op);
    }

    internal void AddPrefix(RawOp op)
    {
        _input = null;
        _rightInput.Add(op);
    }







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
        _leftInput.Add(op);
        return true;
    }

    internal bool TryAddPostfix(RawOp op)
    {
        if (!IsPostfix())
            return false;
        PostFixed = true;
        _rightInput.Insert(0, op);
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
