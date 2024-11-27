using FlexibleParsingLanguage.Modules;
using System.Collections;
using System.ComponentModel;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using static System.Formats.Asn1.AsnWriter;

namespace FlexibleParsingLanguage;

internal enum ParseOperationType
{
    UnbranchRead,

    WriteInitRootMap,
    WriteInitRootArray,

    WriteRoot,
    WriteLoad,
    WriteSave,
    WriteAccess,
    WriteAccessInt,
    WriteFromRead,
    WriteForeach,
    AddFromRead,
    ReadRoot,
    ReadLoad,
    ReadSave,
    ReadAccess,
    ReadAccessInt,
    ReadForeach,
}

internal class ParseOperation
{
    internal ParseOperationType OpType { get; set; }
    internal string StringAcc { get; set; }
    internal int IntAcc { get; set; }


    internal ParseOperation(ParseOperationType opType, string acc = null)
    {
        this.OpType = opType;
        this.StringAcc = acc;
        this.IntAcc = -1;
    }

    internal ParseOperation(ParseOperationType opType, int acc)
    {
        this.OpType = opType;
        this.IntAcc = acc;
    }
}

public interface IReadingModule
{
    public List<Type> HandledTypes { get; }
    public object Parse(object raw, string acc);
    public object Parse(object raw, int acc);

    public IEnumerable Foreach(object raw);
}

public interface IWritingModule
{
    public List<Type> HandledTypes { get; }
    public object BlankMap();
    public object BlankArray();
    public void Write(object target, string acc, object? val);
    public void Write(object target, int acc, object? val);
    public void Append(object target, object? val);
}

public class Parser
{
    private List<ParseOperation> _ops;
    private ModuleHandler _modules;


    internal Parser(List<ParseOperation> ops)
    {
        _ops = ops;
        _modules = new ModuleHandler([
            new JsonParsingModule(),
            new CollectionParsingModule()
        ]);
    }

    public object Parse(object readRoot)
    {
        IWritingModule writer = new CollectionWritingModule();
        var store = new Dictionary<int, object>();

        object writeRoot = null;
        object writeHead = null;

        var readContext = new ReadContext(_modules, readRoot);

        foreach (var o in _ops)
        {
            switch (o.OpType)
            {
                case ParseOperationType.WriteInitRootArray:
                    writeRoot = writer.BlankArray();
                    writeHead = writeRoot;
                    break;
                case ParseOperationType.WriteInitRootMap:
                    writeRoot = writer.BlankMap();
                    writeHead = writeRoot;
                    break;
                case ParseOperationType.WriteRoot:
                    writeHead = writeRoot;
                    break;
                case ParseOperationType.WriteAccessInt:
                    var w1 = writer.BlankArray();
                    writer.Write(writeHead, o.IntAcc, w1);
                    writeHead = w1;
                    break;
                case ParseOperationType.WriteAccess:
                    var w2 = writer.BlankMap();
                    writer.Write(writeHead, o.StringAcc, w2);
                    writeHead = w2;
                    break;
                case ParseOperationType.WriteFromRead:
                    writer.Write(writeHead, o.StringAcc, readContext.ReadHead);
                    break;
                case ParseOperationType.ReadRoot:
                    readContext.ReadHead = readContext.ReadRoot;
                    break;
                case ParseOperationType.ReadAccess:
                    readContext.ReadAction((m, readSrc) => m.Parse(readSrc, o.StringAcc));
                    break;
                case ParseOperationType.ReadAccessInt:
                    readContext.ReadAction((m, readSrc) => m.Parse(readSrc, o.IntAcc));
                    break;
                case ParseOperationType.WriteLoad:
                    writeHead = store[o.IntAcc];
                    break;
                case ParseOperationType.ReadLoad:
                    readContext.ReadAction((m, readSrc) => store[o.IntAcc]);
                    break;
                case ParseOperationType.WriteSave:
                    store[o.IntAcc] = writeHead;
                    break;
                case ParseOperationType.ReadSave:
                    store[o.IntAcc] = readContext.ReadHead;
                    break;
                case ParseOperationType.AddFromRead:
                    writer.Append(writeHead, readContext.ReadHead);
                    break;
                case ParseOperationType.WriteForeach:
                    break;
                case ParseOperationType.ReadForeach:
                    readContext.ReadFlatten();
                    break;
                case ParseOperationType.UnbranchRead:
                    readContext.UnbranchRead();
                    break;
            }
        }
        return writeRoot;
    }
}