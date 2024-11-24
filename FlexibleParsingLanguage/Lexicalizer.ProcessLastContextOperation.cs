using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage;

internal partial class Lexicalizer
{
    private void ProcessLastContextOperation(ParseData parser, ParseContext context, AccessorData data)
    {
        var nextToken = data.NextActiveChar();

        if (context.WriteMode == WriteMode.Read)
        {
            return;


            //if (context.WriteId == WRITE_ROOT && !opsMap.ContainsKey(new OperatorKey(-1, ROOT, null, true)))
            //  ops.Add(new ParseOperation(ParseOperationType.WriteInitRootArray));
            //ops.Add(new ParseOperation(ParseOperationType.AddFromRead));
        }



        EnsureWriteRootExists(parser, context, data);




        parser.Ops.Add(new ParseOperation(ParseOperationType.WriteSave, context.WriteId));

        if (context.WriteMode == WriteMode.Read)
            parser.Ops.Add(new ParseOperation(ParseOperationType.AddFromRead));






        if (nextToken == ' ')
        {
            parser.Ops.Add(new ParseOperation(ParseOperationType.WriteFromRead, data.Accessor));
            return;
        }













        /*

        context.WriteMode = WriteMode.Written;
        if (context.WriteId == WRITE_ROOT && !opsMap.ContainsKey(new OperatorKey(-1, ROOT, null, true)))
        {
            opsMap.Add(new OperatorKey(-1, ROOT, null, true), WRITE_ROOT);
            if (data.Numeric || context.WriteMode == WriteMode.Read)
                ops.Add(new ParseOperation(ParseOperationType.WriteInitRootArray));
            else
                ops.Add(new ParseOperation(ParseOperationType.WriteInitRootMap));
            loadedWriteId = WRITE_ROOT;
        }


        var nextToken = data.NextActiveChar();

        if (nextToken == ' ')
        {
            ops.Add(new ParseOperation(ParseOperationType.WriteFromRead, data.Accessor));
            return;
        }

        var key = new OperatorKey(context.WriteId, data.Operator, data.Accessor, true);

        if (opsMap.TryGetValue(key, out var writeId))
        {
            context.WriteId = writeId;
            return;
        }

        if (context.WriteId != loadedWriteId)
        {
            ops.Add(new ParseOperation(ParseOperationType.WriteLoad, context.WriteId));
            loadedWriteId = context.WriteId;
        }

        context.WriteId = ++idCounter;
        loadedWriteId = context.WriteId;
        opsMap.Add(key, context.WriteId);


        if (nextToken == '[')
        {
            if (!int.TryParse(data.Accessor, out var id))
                throw new ArgumentException("");
            ops.Add(new ParseOperation(ParseOperationType.WriteAccess, data.Accessor));
        }
        else
        {
            ops.Add(new ParseOperation(ParseOperationType.WriteAccess, data.Accessor));
        }
        */
    }
}
