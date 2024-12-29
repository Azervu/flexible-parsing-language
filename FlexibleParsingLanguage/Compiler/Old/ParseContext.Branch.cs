using FlexibleParsingLanguage.Parse;

namespace FlexibleParsingLanguage.Compiler;

internal partial class ParseContext
{
    internal OpCompileType ProcessBranch(ParseData parser)
    {
        var startActiveId = parser.ActiveId;
        if (ChildOperator == null)
            throw new Exception("Context branch missing accessors");

        OpCompileType? writeType = null;

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
            if (writeType == OpCompileType.WriteObject)
                throw new Exception("Branch-B");

            HandleOp(parser, new ParseOperation(ParsesOperationType.WriteAddRead));
        }

        parser.ActiveId = startActiveId;

        return writeType ?? OpCompileType.WriteArray;
    }
}