using FlexibleParsingLanguage.Parse;

namespace FlexibleParsingLanguage.Compiler;

internal partial class ParseContext
{

    /*


                        case "~":
                        if (parent.WriteMode == WriteMode.Read)
                            yield return new ParseOperation(ParseOperationType.ReadName);
                        else
                            yield return new ParseOperation(ParseOperationType.WriteNameFromRead);
                        break;

    */

    internal WriteType? Process(ParseData parser, bool finalContextOp)
    {
        switch (Token.Type?.Operator)
        {
            case "|":
                HandleOp(parser, new ParseOperation(ParsesOperationType.Function, Token.Accessor));
                break;
            case "~":
                HandleOp(parser, new ParseOperation(ParsesOperationType.ReadName));
                break;
            case "{":
                return ProcessBranch(parser);
            case "*":
                var o = new ParseOperation(ParsesOperationType.ReadFlatten, Token.Accessor);
                HandleOp(parser, o);
                break;
            case "#":
                return ProcessLookup(parser);
            case "##":
                return ProcessContextLookup(parser);

            case ":":
                return ProcessWrite(parser, finalContextOp);
            case ".":
            case "\"":
            case "'":
            case "[":
                HandleOp(parser, new ParseOperation(ParsesOperationType.Read, Token.Accessor));
                break;
        }
        return null;
    }



    private void HandleOp(ParseData parser, ParseOperation? op)
    {
    }

    private void HandleOps(ParseData parser, ParseOperation[] ops)
    {
    }
}












/*
switch (ctx.Operator)
{

    case "$":
        if (parent.WriteMode == WriteMode.Read)
            yield return new ParseOperation(ParseOperationType.ReadRoot);
        else
            yield return new ParseOperation(ParseOperationType.WriteRoot);
        break;
    case "€":
        foreach (var op in ProcessLookupOperation(config, data, parent, ctx))
            yield return op;
        break;
    case "~":
        if (parent.WriteMode == WriteMode.Read)
            yield return new ParseOperation(ParseOperationType.ReadName);
        else
            yield return new ParseOperation(ParseOperationType.WriteNameFromRead);
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
