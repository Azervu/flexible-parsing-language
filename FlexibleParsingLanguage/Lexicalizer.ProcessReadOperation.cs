using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage;

internal partial class Lexicalizer
{
    private void ProcessReadOperator(ParseData parser, ParseContext context, AccessorData data)
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



        var key = new OperatorKey(context.ReadId, data.Operator, data.Accessor, false);
        if (parser.OpsMap.TryGetValue(key, out var readId))
        {
            context.ReadId = readId;
            return;
        }


        {
            if (key.TargetId != parser.LoadedReadId)
            {
                if (key.TargetId == READ_ROOT)
                {
                    parser.Ops.Add(new ParseOperation(ParseOperationType.ReadRoot));
                }
                else
                {
                    parser.Ops.Add(new ParseOperation(ParseOperationType.ReadLoad, key.TargetId));
                    parser.LoadedReadId = key.TargetId;
                }
            }
        }

        context.ReadId = ++parser.IdCounter;
        parser.LoadedReadId = context.ReadId;
        parser.OpsMap.Add(key, context.ReadId);
        parser.Ops.Add(new ParseOperation(ParseOperationType.ReadAccess, data.Accessor));
        parser.Ops.Add(new ParseOperation(ParseOperationType.ReadSave, context.ReadId));
    }
}