using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage;

internal partial class Lexicalizer
{
    private void ProcessReadOperator(ParseData parser, ParseContext ctx, AccessorData data)
    {

        /*
        if (numeric)
        {
            if (!int.TryParse(accessor, out var i))
                throw new ArgumentException($"non numeric accessor {accessor}");
            if (writeMode)
                var k  =new ParseOperation(ParseOperationType.WriteAccessInt, i);
            else
                var k = new ParseOperation(ParseOperationType.WriteAccessInt, i);
        }
        */

        //TODO enum flags write / numeric etc
        //TODO invert write logic - read out in - write in out



        var key = new OperatorKey(ctx.ReadId, data.Operator, data.Accessor, false);
        if (parser.OpsMap.TryGetValue(key, out var readId))
        {
            ctx.ReadId = readId;
            return;
        }


        EnsureReadOpLoaded(parser, ctx);
        ctx.ReadId = ++parser.IdCounter;
        parser.SaveOps.Add(ctx.ReadId);
        parser.LoadedReadId = ctx.ReadId;
        parser.OpsMap.Add(key, ctx.ReadId);
        parser.Ops.Add(new ParseOperation(ParseOperationType.ReadAccess, data.Accessor));
        parser.Ops.Add(new ParseOperation(ParseOperationType.ReadSave, ctx.ReadId));
    }
}