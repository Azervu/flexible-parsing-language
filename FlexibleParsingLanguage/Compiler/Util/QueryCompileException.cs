using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler.Util
{
    public class QueryCompileException : Exception
    {
        internal RawOp Op { get; private set; }
        internal QueryCompileException(RawOp op, string message) : base(message)
        {
            Op = op;

        }

        public string GenerateMessage(string query)
        {

            var indicator = Op.CharIndex >= 0
                ? "\n" + new string(' ', Op.CharIndex) + '↓'
                : "" ;

            return $"{Op.Type.Operator}({Op.Id}) | message = {Message}{indicator}\n{query}";
        }

    }
}
