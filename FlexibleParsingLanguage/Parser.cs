using System.Text.Json;
using System.Text.Json.Nodes;

namespace FlexibleParsingLanguage;

internal enum ParseOperationType
{
    WriteInitRootMap,
    WriteInitRootArray,

    WriteRoot,
    WriteLoad,
    WriteSave,
    WriteAccess,
    WriteAccessInt,
    WriteFromRead,
    AddFromRead,
    ReadRoot,
    ReadLoad,
    ReadSave,
    ReadAccess,
    ReadAccessInt,
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

    private List<(List<Type>, IReadingModule)> _modules;

    private Dictionary<Type, IReadingModule> _moduleLookup;

    internal Parser(List<ParseOperation> ops)
    {
        _ops = ops;
        _modules = new List<(List<Type>, IReadingModule)>
        {
            ([typeof(JsonNode)] , new JsonParsingModule() )
        };
        _moduleLookup = new Dictionary<Type, IReadingModule>();
    }

    private IReadingModule LookupModule(Type t)
    {
        if (_moduleLookup.TryGetValue(t, out var m))
            return m;

        foreach (var (types, m2) in _modules)
        {
            foreach (var mt in types)
            {
                if (mt.IsAssignableFrom(t))
                {
                    _moduleLookup.Add(t, m2);
                    return m2;
                }
            }
        }

        throw new Exception($"{t.Name} not supported by parser");
    }

    public object Parse(object readRoot)
    {
        IWritingModule writer = new CollectionWritingModule();
        IReadingModule reader = null;
        var store = new Dictionary<int, object>();

        object writeRoot = null;
        object readHead = readRoot;
        object writeHead = null;
        Type activeType = null;



        var x = "";

        foreach (var o in _ops)
        {
            var debug = _ops.Select(x => $"{(x == o ? "*" : " ")} {x.OpType} {x.IntAcc} {x.StringAcc} ").Join("\n");

            x += "\n" + JsonSerializer.Serialize(readHead) + $" | {o.OpType} {o.IntAcc} {o.StringAcc}";


            var t = readHead?.GetType() ?? typeof(void);
            if (t != activeType)
            {
                activeType = t;
                reader = LookupModule(t);
            }
            ParseInner(
                writer,
                reader,
                store,
                ref readRoot,
                ref writeRoot,
                ref readHead,
                ref writeHead,
                o
            );


            if (readHead == null)
            {
                throw new Exception($"Active read result in null | op = {o.OpType} | acc = {o.StringAcc} | i = {o.IntAcc}");
            }

        }
        return writeRoot;
    }

    private void ParseInner(
        IWritingModule write,
        IReadingModule activeModule,
        Dictionary<int, object> stored,
        ref object readRoot,
        ref object writeRoot,
        ref object readHead,
        ref object writeHead,
        ParseOperation o
        )
    {
        switch (o.OpType)
        {
            case ParseOperationType.WriteInitRootArray:
                writeRoot = write.BlankArray();
                writeHead = writeRoot;
                break;
            case ParseOperationType.WriteInitRootMap:
                writeRoot = write.BlankMap();
                writeHead = writeRoot;
                break;
            case ParseOperationType.WriteRoot:
                writeHead = writeRoot;
                break;
            case ParseOperationType.WriteAccessInt:
                var w1 = write.BlankArray();
                write.Write(writeHead, o.IntAcc, w1);
                writeHead = w1;
                break;
            case ParseOperationType.WriteAccess:
                var w2 = write.BlankMap();
                write.Write(writeHead, o.StringAcc, w2);
                writeHead = w2;
                break;
            case ParseOperationType.WriteFromRead:
                write.Write(writeHead, o.StringAcc, readHead);
                //write.Write(ref writeHead, o.IntAcc, readHead);
                break;
            case ParseOperationType.ReadRoot:
                readHead = readRoot;
                break;
            case ParseOperationType.ReadAccess:
                readHead = activeModule.Parse(readHead, o.StringAcc);
                break;
            case ParseOperationType.ReadAccessInt:
                readHead = activeModule.Parse(readHead, o.IntAcc);
                break;
            case ParseOperationType.WriteLoad:
                writeHead = stored[o.IntAcc];
                break;
            case ParseOperationType.ReadLoad:
                readHead = stored[o.IntAcc];
                break;
            case ParseOperationType.WriteSave:
                stored[o.IntAcc] = writeHead;
                break;
            case ParseOperationType.ReadSave:
                stored[o.IntAcc] = readHead;
                break;
            case ParseOperationType.AddFromRead:
                write.Append(writeHead, readHead);
                break;
        }
    }
}