using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler.Util
{
    public class QueryCompileException : Exception
    {
        public bool CompilerIssue { get; private set; }

        internal List<RawOp> Ops { get; private set; }
        internal QueryCompileException(RawOp op, string message, bool compilerIssue = false) : base(message) {
            Ops = new List<RawOp> { op };
            CompilerIssue = compilerIssue;
        }

        internal QueryCompileException(List<RawOp> ops, string message, bool compilerIssue = false) : base(message) {
            Ops = ops;
            CompilerIssue = compilerIssue;
        }

        public string GenerateMessage(string query)
        {
            var log = new StringBuilder(Ops.Count > 0
                ? $"{Ops[0].Type.Operator}({Ops[0].Id}) | message = {Message}"
                : Message
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
            log.Append(query);
            return log.ToString();
        }
    }
}

/*
 
Assert.Fail failed. {(4) | message = Could not resolve dependencies
 ↓   ↓   ↓
a{@b1:h2}b2:h1

*/