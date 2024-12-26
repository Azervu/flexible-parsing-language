using System.Text;

namespace FlexibleParsingLanguage.Compiler.Util;

internal class RawOp
{
    internal int Id { get; set; }
    internal int CharIndex { get; set; }
    internal OpConfig Type { get; set; }
    internal string? Accessor { get; set; }
    internal List<RawOp> LeftInput { get; private set; } = new List<RawOp>();
    internal List<RawOp> RightInput { get; private set; } = new List<RawOp>();
    internal IEnumerable<RawOp> GetRawInput() => LeftInput.Concat(RightInput);
    internal List<RawOp> Output { get; set; } = new List<RawOp>();
    internal bool Prefixed { get; set; }
    internal bool PostFixed { get; set; }


    internal bool IsPrefix()
    {
        if (Type.Category.All(OpCategory.RightInput) && !Prefixed)
            return true;

        return false;
    }

    internal bool IsPostfix()
    {
        if (Type.Category.All(OpCategory.LeftInput) && !PostFixed)
            return true;

        return false;
    }

    internal List<RawOp> Input = new List<RawOp>();

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

        if (Input != null)
        {
            foreach (var c in Input)
                c.AddLog(log, depth + 1);
        }
    }

    public override string ToString() => ToString(new StringBuilder()).ToString();

    internal StringBuilder ToString(StringBuilder l)
    {
        l.Append(Id.ToString());
        l.Append(string.IsNullOrEmpty(Type.Operator) ? $"'{Accessor.Replace("'", "\\'")}'" : Type.Operator);
        if (Accessor != null)
            l.Append($"\"{Accessor}\"");

        var input = GetRawInput().ToList();
        if (input.Count > 0)
        {
            l.Append('(');
            l.Append(input.Select(x => {
                if (x.IsSimple())
                    return (string.IsNullOrEmpty(x.Type.Operator) ? $"'{x.Accessor.Replace("'", "\\'")}'" : x.Type.Operator);

                return x.Id.ToString();
            }).Join(","));
            l.Append(")");
        }
        return l;
    }
}

internal static class RawOpExtension
{
    internal static string RawQueryToString(this List<RawOp> parsed)
    {

        var log = new StringBuilder();

        var proccessed = new HashSet<int>();

        foreach (var t in parsed)
            LogEntry(proccessed, log, t);

        return log.ToString();
    }


    internal static bool IsSimple(this RawOp x) => x.Output.Count == 1 && x.GetRawInput().Count() == 0;

    private static void LogEntry(HashSet<int> proccessed, StringBuilder log, RawOp t)
    {
        if (proccessed.Contains(t.Id) || t.IsSimple())
            return;

        var input = t.GetRawInput().ToList();
        proccessed.Add(t.Id);
        foreach (var inp in t.GetRawInput())
            LogEntry(proccessed, log, inp);

        if (log.Length > 0)
            log.Append("  ");

        t.ToString(log);








        /*
        log.Append($"{t.Id}");
        log.Append(string.IsNullOrEmpty(t.Type.Operator) ? $"'{t.Accessor.Replace("'", "\\'")}'" : t.Type.Operator);

        if (input.Count > 0)
        {
            log.Append($"[{input.Select(x => {
                if (x.IsSimple())
                    return string.IsNullOrEmpty(x.Type.Operator) ? $"'{x.Accessor.Replace("'", "\\'")}'" : x.Type.Operator;

                return x.Id.ToString();
            }).Join(",")}");
            log.Append($"]");
        }
        */
    }
}