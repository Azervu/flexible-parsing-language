using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler;

public class QueryException : Exception
{
    public bool CompilerIssue { get; private set; }

    internal string Query { get; set; }

    internal string RawQuery { get; set; }

    internal string RawMessage { get; set; }

    internal List<RawOp> Ops { get; private set; }
    internal QueryException
        (RawOp op, string message, bool compilerIssue = false, Exception ex = null) : base(message, ex) {
        RawMessage = message;
        Ops = new List<RawOp> { op };
        CompilerIssue = compilerIssue;
    }

    internal QueryException(List<RawOp> ops, string message, bool compilerIssue = false) : base(message) {
        RawMessage = message;
        Ops = ops;
        CompilerIssue = compilerIssue;
    }

    internal string _message;
    public override string Message
    {
        get
        {
            if (_message == null)
                _message = GenerateMessage();
            return _message;
        }
    }

    public string GenerateMessage()
    {
        var log = new StringBuilder(Ops.Count > 0
            ? $" | op = {Ops[0].Type.Operator}{(string.IsNullOrEmpty(Ops[0].Accessor) ? string.Empty : $"'{Ops[0].Accessor}'")}({Ops[0].Id}) | origQuery = {RawQuery} | message = {base.Message}\n{InnerException?.StackTrace.ToString()}"
            : base.Message
        );

        var indices = Ops.Where(x => x.CharIndex >= 0).Select(x => x.CharIndex).Distinct().Order().ToList();
        if (indices.Any())
        {
            var lastIndex = -1;
            log.Append("\n");
            foreach (var i in indices)
            {
                log.Append(new string(' ', i - lastIndex -1) + '↓');
                lastIndex = i;
            }
        }


        log.Append("\n");
        log.Append(Query);
        return log.ToString();
    }
}