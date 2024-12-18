using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FlexibleParsingLanguage.Compiler;

internal partial class ParseContext
{
    internal WriteType ProcessContext(ParseData parser)
    {
        if (Accessors != null)
        {

        }
        else if (Token.Acc != null)
        {

        }


        /*
         

        k:h:@




        
                if (parent.WriteMode == WriteMode.Read)
                {
                    
                }
                else if (parent.LastWriteOp)
                {
                    parent.ProcessedEnd = true;
                    yield return ProcessContextEndingOperator(config, data, parent, ctx);
                }
                else
                {
                    yield return ProcessWriteOperator(config, data, parent, ctx);
                }


         */



        WriteType? writeStatus = null;




        switch (Token.Op?.Operator)
        {
            case "{":
                return ProcessContextBranch(parser);
            case ":":
                HandleOp(parser, this, new ParseOperation(ParseOperationType.WriteFromRead, Param));
                return WriteType.Object;
            case ".":
                HandleOp(parser, this, new ParseOperation(ParseOperationType.Read, Param));
                return WriteType.Object;
        }

                /*
                switch (Token.Op?.Operator)
                {
                    case "|":
                        yield return new ParseOperation(ParseOperationType.Function, Accessor);
                        break;
                    case "$":
                        if (parent.WriteMode == WriteMode.Read)
                            yield return new ParseOperation(ParseOperationType.ReadRoot);
                        else
                            yield return new ParseOperation(ParseOperationType.WriteRoot);
                        break;
                    case ":":
                        yield return new ParseOperation(ParseOperationType.LookupRead);
                        break;
                    case "€":
                        foreach (var op in ProcessLookupOperation(config, data, parent, ctx))
                            yield return op;
                        break;
                    case "*":
                        if (parent.WriteMode == WriteMode.Read)
                        {
                            yield return new ParseOperation(ParseOperationType.ReadFlatten, ctx.Accessor);
                        }
                        else
                        {
                            var nextOp = parent.NextReadOperator();
                            var nextNumeric = nextOp?.Numeric ?? true;
                            yield return new ParseOperation(nextNumeric ? ParseOperationType.WriteFlattenArray : ParseOperationType.WriteFlattenObj);
                        }
                        break;
                    case "~":
                        if (parent.WriteMode == WriteMode.Read)
                            yield return new ParseOperation(ParseOperationType.ReadName);
                        else
                            yield return new ParseOperation(ParseOperationType.WriteNameFromRead);
                        break;
                    case ".":
                    case "\"":
                    case "'":
                    case "[":
                        yield return new ParseOperation(ParseOperationType.Read, Accessor);
                        break;
                    default:
                        break;
                }
                */




/*






        */



        return WriteType.Array;
    }




    internal WriteType ProcessContextBranch(ParseData parser)
    {

        var startActiveId = parser.ActiveId;



        if (Accessors == null)
            throw new Exception("Context branch missing accessors");

        var lastWriteIndex = -1;
        for (var i = Accessors.Count - 1; i >= 0; i--)
        {
            var accessor = Accessors[i];
            if (accessor.Param == ":")
            {
                lastWriteIndex = i;
                break;
            }
        }

        WriteType? writeType = null;

        for (var i = 0; i < Accessors.Count; i++)
        {
            if (i == lastWriteIndex)
                continue;
            var accessor = Accessors[i];

            var wt = accessor.ProcessContext(parser);

            if (writeType != null && writeType != wt)
                throw new Exception("***************");

            writeType = wt;
        }


        if (lastWriteIndex < 0)
        {
            HandleOp(parser, this, new ParseOperation(ParseOperationType.WriteAddRead));
            parser.ActiveId = startActiveId;
            return WriteType.Array;
        }

    
        HandleOp(parser, this, new ParseOperation(ParseOperationType.WriteFromRead, Param));
        parser.ActiveId = startActiveId;

        //foreach (var op in ProcessEndingOperation(parser, this, lastWriteIndex >= 0 ? Accessors[lastWriteIndex] : null))



        return WriteType.Object;
    }







        /*
        if ()



        switch (ctx.Operator)
        {
            case "|":
                yield return new ParseOperation(ParseOperationType.Function, ctx.Accessor);
                break;
            case "$":
                if (parent.WriteMode == WriteMode.Read)
                    yield return new ParseOperation(ParseOperationType.ReadRoot);
                else
                    yield return new ParseOperation(ParseOperationType.WriteRoot);
                break;
            case ":":
                parent.WriteMode = WriteMode.Write;
                break;
            case "€":
                foreach (var op in ProcessLookupOperation(config, data, parent, ctx))
                    yield return op;
                break;
            case "*":
                if (parent.WriteMode == WriteMode.Read)
                {
                    yield return new ParseOperation(ParseOperationType.ReadFlatten, ctx.Accessor);
                }
                else
                {
                    var nextOp = parent.NextReadOperator();
                    var nextNumeric = nextOp?.Numeric ?? true;
                    yield return new ParseOperation(nextNumeric ? ParseOperationType.WriteFlattenArray : ParseOperationType.WriteFlattenObj);
                }
                break;
            case "~":
                if (parent.WriteMode == WriteMode.Read)
                    yield return new ParseOperation(ParseOperationType.ReadName);
                else
                    yield return new ParseOperation(ParseOperationType.WriteNameFromRead);
                break;
            case ".":
            case "\"":
            case "'":
            case "[":
                if (parent.WriteMode == WriteMode.Read)
                {
                    yield return new ParseOperation(ParseOperationType.Read, ctx.Accessor);
                }
                else if (parent.LastWriteOp)
                {
                    parent.ProcessedEnd = true;
                    yield return ProcessContextEndingOperator(config, data, parent, ctx);
                }
                else
                {
                    yield return ProcessWriteOperator(config, data, parent, ctx);
                }
                break;
            default:
                break;
        }
        */



    /*

    private IEnumerable<ParseOperation> ProcessContextEndingOperator(ParserConfig config, ParseData data, ParseContext ctx, ParseContext? acc)
    {





        if (ctx.Accessors.Count != 0 && ctx.Accessors.Last() != null)
            yield break;


        EnsureWriteOpLoaded(config, data, ctx, acc);

        if (acc == null || ctx.WriteMode == WriteMode.Read)
            return new ParseOperation(ParseOperationType.WriteAddRead);
        else if (acc.Numeric)
            return new ParseOperation(ParseOperationType.WriteFromRead); //Int
        else
            return new ParseOperation(ParseOperationType.WriteFromRead, acc.Accessor);
    }
    */























    /*




IEnumerable<ParseOperation> ProcessEndingOperation(ParseData data, ParseContext parentContext, ParseContext? ctx)
{
    if (ctx == null)
        yield return new ParseOperation(ParseOperationType.WriteAddRead);




    EnsureReadOpLoaded(data, ctx);
    EnsureWriteOpLoaded(config, data, ctx, accessor);

    if (ctx == null)
        yield return new ParseOperation(ParseOperationType.Read, accessor.Accessor);


}
    */

    private void HandleOp(ParseData parser, ParseContext ctx, ParseOperation? op)
    {
        if (op == null)
            return;

        var activeId = op.OpType == ParseOperationType.ReadRoot ? -1 : parser.ActiveId;
        var key = (activeId, op);
        if (parser.OpsMap.TryGetValue(key, out var readId))
        {
            parser.ActiveId = readId;
            return;
        }

        if (parser.ActiveId != parser.LoadedId)
        {
            if (!parser.SaveOps.Contains(parser.ActiveId))
                throw new Exception("Query parsing error | Unknown read id " + parser.ActiveId);
            parser.Ops.Add((-1, new ParseOperation(ParseOperationType.Load, parser.ActiveId)));
            parser.LoadedId = parser.ActiveId;
        }


        parser.ActiveId = ++parser.IdCounter;
        parser.SaveOps.Add(parser.ActiveId);
        parser.LoadedId = parser.ActiveId;

        if (parser.OpsMap.ContainsKey(key))
            throw new Exception($"Repeated {key.activeId} ");

        parser.Ops.Add((parser.ActiveId, op));
        parser.OpsMap.Add(key, parser.ActiveId);
        parser.IdCounter++;
        var saveOp = new ParseOperation(ParseOperationType.Save, parser.ActiveId);
        parser.Ops.Add((parser.IdCounter, saveOp));
        parser.OpsMap.Add((activeId, saveOp), parser.IdCounter);
    }
}

