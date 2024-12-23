using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler.Util
{
    public class QueryCompileException : Exception
    {
        internal List<RawOp> Ops { get; private set; }
        internal QueryCompileException(RawOp op, string message) : base(message) { Ops = new List<RawOp> { op }; }
        internal QueryCompileException(List<RawOp> ops, string message) : base(message) { Ops = ops; }

        public string GenerateMessage(string query)
        {
            var op = Ops[0];
            var log = new StringBuilder($"{op.Type.Operator}({op.Id}) | message = {Message}");

            var indices = Ops.Where(x => x.CharIndex >= 0).Select(x => x.CharIndex).Distinct().Order().ToList();
            if (indices.Any())
            {
                var lastIndex = 0;
                log.Append("\n");
                foreach (var i in indices)
                {
                    log.Append(new string(' ', i - lastIndex) + '↓');
                    lastIndex = i - 1;
                }
            }


            log.Append("\n");
            log.Append(query);
            return log.ToString();
        }
    }
}