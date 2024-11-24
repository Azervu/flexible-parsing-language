using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FlexibleParsingLanguage;

internal partial class Lexicalizer
{


    private void EnsureWriteRootExists(ParseData parser, ParseContext context, AccessorData data)
    {
        if (context.WriteId != WRITE_ROOT)
            return;
        var key = new OperatorKey(-1, ROOT, null, true);

        if (parser.OpsMap.ContainsKey(key))
            return;

        parser.OpsMap.Add(key, WRITE_ROOT);
        if (data.Numeric || context.WriteMode == WriteMode.Read)
            parser.Ops.Add(new ParseOperation(ParseOperationType.WriteInitRootArray));
        else
            parser.Ops.Add(new ParseOperation(ParseOperationType.WriteInitRootMap));
        parser.LoadedWriteId = WRITE_ROOT;
    }


    private void ProcessWriteOperator(ParseData parser, ParseContext context, AccessorData data)
    {
        EnsureWriteRootExists(parser, context, data);

        context.WriteMode = WriteMode.Written;
        var nextToken = data.NextActiveChar();
        var key = new OperatorKey(context.WriteId, data.Operator, data.Accessor, true);

        if (parser.OpsMap.TryGetValue(key, out var writeId))
        {
            context.WriteId = writeId;
            return;
        }

        if (context.WriteId != parser.LoadedWriteId)
        {
            parser.Ops.Add(new ParseOperation(ParseOperationType.WriteLoad, context.WriteId));
            parser.LoadedWriteId = context.WriteId;
        }

        context.WriteId = ++parser.IdCounter;
        parser.LoadedWriteId = context.WriteId;
        parser.OpsMap.Add(key, context.WriteId);


        if (nextToken == '[')
        {
            if (!int.TryParse(data.Accessor, out var id))
                throw new ArgumentException("");
            parser.Ops.Add(new ParseOperation(ParseOperationType.WriteAccess, data.Accessor));
        }
        else
        {
            parser.Ops.Add(new ParseOperation(ParseOperationType.WriteAccess, data.Accessor));
        }
    }
}
