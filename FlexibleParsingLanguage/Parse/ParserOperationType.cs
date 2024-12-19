using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Parse;

internal enum ParseOperationType
{
    Save,
    Load,
    Function,
    ReadRoot,
    Read,
    ReadInt,
    ReadFlatten,
    ReadName,
    Write,
    WriteRoot,
    WriteInt,
    WriteArray,
    WriteArrayInt,
    WriteFlatten,
    WriteFromRead,
    WriteAddRead,
    WriteNameFromRead,


    ParamLiteral,
    ParamToRead,
    ParamFromRead,

    Lookup,
    ChangeLookup,

    LookupRead,
    LookupReadValue,

    LookupLiteral,
    LookupReadAccess,
}




internal struct OperationMetaData
{
    internal WriteType WriteType;

    internal OperationMetaData(WriteType wt)
    {
        WriteType = wt;
    }
}

internal static class ParserOperationType2
{
    internal static OperationMetaData GetMetaData(Action<Parser, ParsingContext, int, string> op) => Ops[op];

    internal static Dictionary<Action<Parser, ParsingContext, int, string>, OperationMetaData> Ops = new Dictionary<Action<Parser, ParsingContext, int, string>, OperationMetaData>
    {
        { WriteAddRead, new OperationMetaData(WriteType.None) }
    };



    internal static void WriteAddRead(Parser parser, ParsingContext context, int intAcc, string acc)
    {
        foreach (var focusEntry in context.Focus)
        {
            //UpdateWriteModule(w);
            if (focusEntry.MultiRead)
            {
                foreach (var r in focusEntry.Reads)
                    context.WritingModule.Append(focusEntry.Write, context.TransformReadInner(r.Read));
                continue;
            }
            context.WritingModule.Append(focusEntry.Write, focusEntry.Reads[0].Read);
        }
    }


}












internal static class ParseOperationTypeExtension
{



    internal static WriteType GetWriteType(this ParseOperationType op)
    {
        switch (op)
        {
            case ParseOperationType.Read:
            case ParseOperationType.Write:
                return WriteType.Object;
            case ParseOperationType.ReadInt:
            case ParseOperationType.ReadFlatten:
            case ParseOperationType.WriteAddRead:
            case ParseOperationType.WriteArray:
            case ParseOperationType.WriteArrayInt:
                return WriteType.Array;
            case ParseOperationType.WriteInt:
            case ParseOperationType.WriteFlatten:
            case ParseOperationType.WriteFromRead:
            case ParseOperationType.WriteNameFromRead:
            case ParseOperationType.WriteRoot:
            case ParseOperationType.Function:
            case ParseOperationType.ReadRoot:
            case ParseOperationType.Save:
            case ParseOperationType.Load:
            case ParseOperationType.ReadName:
            default:
                return WriteType.None;
        }
    }

}