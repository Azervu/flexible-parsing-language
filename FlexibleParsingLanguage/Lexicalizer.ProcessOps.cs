﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage;

internal partial class Lexicalizer
{
    private void ProcessWriteOps(
        List<ParseOperation> ops,
        Dictionary<OperatorKey, int> opsMap,
        ref int idCounter,
        ref int writeId,
        ref int readId,
        ref int loadedWriteId,
        ref int loadedReadId,
        List<(char, string?)> writeOps
    )
    {


        var add = !writeOps.Any();




        if (writeId == WRITE_ROOT && !opsMap.ContainsKey(new OperatorKey(-1, ROOT, null, true)))
        {
            opsMap.Add(new OperatorKey(-1, ROOT, null, true), WRITE_ROOT);


            if (add || writeOps[0].Item1 == '[')
            {
                ops.Add(new ParseOperation(ParseOperationType.WriteInitRootArray));
            }
            else
            {
                ops.Add(new ParseOperation(ParseOperationType.WriteInitRootMap));
            }
            loadedWriteId = WRITE_ROOT;
        }



        if (add)
        {
            ops.Add(new ParseOperation(ParseOperationType.AddFromRead));
            return;
        }

        int writeIdToLoad = writeId;
        var i = 0;
        for (i = 0; i < writeOps.Count - 1; i++)
        {
            var (token, accessor) = writeOps[i];
            var key = new OperatorKey(writeId, token, accessor, true);
            if (opsMap.TryGetValue(key, out writeId))
            {
                writeIdToLoad = writeId;
                continue;
            }

            if (writeIdToLoad != loadedWriteId)
            {
                ops.Add(new ParseOperation(ParseOperationType.WriteLoad, writeIdToLoad));
                loadedWriteId = writeIdToLoad;
            }

            writeId = ++idCounter;
            loadedWriteId = writeId;
            opsMap.Add(key, writeId);

            if (writeOps[i + 1].Item1 == '[')
            {
                if (!int.TryParse(accessor, out var id))
                    throw new ArgumentException("");
                ops.Add(new ParseOperation(ParseOperationType.WriteAccess, accessor));
            }
            else
            {
                ops.Add(new ParseOperation(ParseOperationType.WriteAccess, accessor));
            }
        }

        var (t, a) = writeOps.Last();
        writeOps.Clear();
        ops.Add(new ParseOperation(ParseOperationType.WriteFromRead, a));
    }

    private void ProcessReadOps(
        List<ParseOperation> ops,
        Dictionary<OperatorKey, int> opsMap,
        ref int idCounter,
        ref int readId,
        ref int loadedReadId,
        char token,
        bool numeric,
        string accessor
        )
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




        var key = new OperatorKey(readId, token, accessor, false);
        if (opsMap.TryGetValue(key, out readId))
            return;

        if (key.TargetId != loadedReadId)
        {
            ops.Add(new ParseOperation(ParseOperationType.ReadLoad, key.TargetId));
            loadedReadId = key.TargetId;
        }
        readId = ++idCounter;
        loadedReadId = readId;
        opsMap.Add(key, readId);
        ops.Add(new ParseOperation(ParseOperationType.ReadAccess, accessor));
        ops.Add(new ParseOperation(ParseOperationType.ReadSave, readId));


    }
}
