using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler;

internal partial class ParseContext
{
    internal WriteType ProcessBranch(ParseData parser)
    {
        var startActiveId = parser.ActiveId;
        if (ChildOperator == null)
            throw new Exception("Context branch missing accessors");

        WriteType? writeType = null;

        var addHandled = false;

        for (var i = 0; i < ChildOperator.Count; i++)
        {
            var accessor = ChildOperator[i];

            var lastOp = i == ChildOperator.Count - 1;


            var wt = accessor.Process(parser, lastOp);

            if (wt == null)
                continue;

            if (writeType == null)
                writeType = wt;

            if (lastOp)
                addHandled = true;
        }


        if (!addHandled)
        {
            if (writeType == WriteType.Object)
                throw new Exception("Branch-B");

            HandleOp(parser, new ParseOperation(ParsesOperationType.WriteAddRead));
        }

        parser.ActiveId = startActiveId;

        return writeType ?? WriteType.Array;
    }
}