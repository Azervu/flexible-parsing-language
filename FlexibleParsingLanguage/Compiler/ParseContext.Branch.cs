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



        /*


        if (writeType != null)

        var lastWriteIndex = -1;
        for (var i = Accessors.Count - 1; i >= 0; i--)
        {
            var accessor = Accessors[i];
            if (accessor.Operator == ":")
            {
                lastWriteIndex = i;
                break;
            }
        }

        for (var i = 0; i < Accessors.Count; i++)
        {
            if (i == lastWriteIndex)
                continue;
            var accessor = Accessors[i];

            writeType = accessor.Process(parser);
        }




        if (lastWriteIndex < 0)
        {
            HandleOp(parser, this, new ParseOperation(ParseOperationType.WriteAddRead));

            return WriteType.Array;
        }

        HandleOp(parser, this, new ParseOperation(ParseOperationType.WriteFromRead, Accessors[lastWriteIndex].Param));
        //foreach (var op in ProcessEndingOperation(parser, this, lastWriteIndex >= 0 ? Accessors[lastWriteIndex] : null))

        return WriteType.Object;
        */
    }
}
