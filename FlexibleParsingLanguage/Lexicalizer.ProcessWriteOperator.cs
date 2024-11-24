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


    private void ProcessWriteOperator(ParseData data, ParseContext ctx, AccessorData acc)
    {


        ctx.WriteMode = WriteMode.Written;;
        var key = new OperatorKey(ctx.WriteId, acc.Operator, acc.Accessor, true);




        if (data.OpsMap.TryGetValue(key, out var writeId))
        {
            ctx.WriteId = writeId;
            return;
        }

        EnsureWriteOpLoaded(data, ctx, acc);

        ctx.WriteId = ++data.IdCounter;
        data.LoadedWriteId = ctx.WriteId;
        data.OpsMap.Add(key, ctx.WriteId);

        if (acc.Numeric)
        {
            if (!int.TryParse(acc.Accessor, out var id))
                throw new ArgumentException("");
            data.Ops.Add(new ParseOperation(ParseOperationType.WriteAccess, acc.Accessor));
        }
        else
        {
            data.Ops.Add(new ParseOperation(ParseOperationType.WriteAccess, acc.Accessor));
        }
    }
}
