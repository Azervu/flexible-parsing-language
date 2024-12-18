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
    WriteFlattenObj,
    WriteFlattenArray,
    WriteFromRead,
    WriteAddRead,
    WriteNameFromRead,
    LookupRead,
    LookupReadValue,

    LookupLiteral,
    LookupReadAccess,
}


internal static class ParseOperationTypeExtension
{
    internal static bool IsWriteOperation(this ParseOperationType op)
    {
        switch (op)
        {
            case ParseOperationType.Write:
            case ParseOperationType.WriteInt:
            case ParseOperationType.WriteArray:
            case ParseOperationType.WriteArrayInt:
            case ParseOperationType.WriteFlattenObj:
            case ParseOperationType.WriteFlattenArray:
            case ParseOperationType.WriteFromRead:
            case ParseOperationType.WriteAddRead:
            case ParseOperationType.WriteNameFromRead:
            case ParseOperationType.WriteRoot:
                return true;
            case ParseOperationType.Function:
            case ParseOperationType.ReadRoot:
            case ParseOperationType.Save:
            case ParseOperationType.Load:
            case ParseOperationType.Read:
            case ParseOperationType.ReadInt:
            case ParseOperationType.ReadFlatten:
            case ParseOperationType.ReadName:
            default:
                return false;
        }
    }

    internal static bool IsReadOperation(this ParseOperationType op)
    {
        switch (op)
        {
            case ParseOperationType.Write:
            case ParseOperationType.WriteInt:
            case ParseOperationType.WriteArray:
            case ParseOperationType.WriteArrayInt:
            case ParseOperationType.WriteFlattenObj:
            case ParseOperationType.WriteFlattenArray:
            case ParseOperationType.WriteFromRead:
            case ParseOperationType.WriteAddRead:
            case ParseOperationType.WriteNameFromRead:
            case ParseOperationType.Save:
            case ParseOperationType.Load:
            case ParseOperationType.WriteRoot:
            default:
                return false;
            case ParseOperationType.ReadRoot:
            case ParseOperationType.Read:
            case ParseOperationType.ReadInt:
            case ParseOperationType.ReadFlatten:
            case ParseOperationType.ReadName:
            case ParseOperationType.Function:
                return true;
        }
    }

    internal static bool IsNumericOperation(this ParseOperationType op)
    {
        switch (op)
        {
            case ParseOperationType.WriteInt:
            case ParseOperationType.WriteArrayInt:
            case ParseOperationType.ReadInt:
            case ParseOperationType.ReadFlatten:
                return true;

            case ParseOperationType.Write:
            case ParseOperationType.WriteRoot:
            case ParseOperationType.WriteArray:
            case ParseOperationType.WriteFlattenObj:
            case ParseOperationType.WriteFlattenArray:
            case ParseOperationType.WriteFromRead:
            case ParseOperationType.WriteAddRead:
            case ParseOperationType.WriteNameFromRead:
            case ParseOperationType.Save:
            case ParseOperationType.Load:
            case ParseOperationType.ReadRoot:
            case ParseOperationType.Read:
            case ParseOperationType.ReadName:
            case ParseOperationType.Function:
            default:
                return false;
        }
    }
}