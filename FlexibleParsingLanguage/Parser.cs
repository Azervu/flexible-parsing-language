using System.Collections;
using System.ComponentModel;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using static System.Formats.Asn1.AsnWriter;

namespace FlexibleParsingLanguage;

internal enum ParseOperationType
{
    UnbranchForeach,


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


        var i = 0;
        ParseInner(
            store,
            writer,
            ref reader,
            ref readRoot,
            ref writeRoot,
            ref readHead,
            ref writeHead,
            ref activeType,
            ref i,
            0
        );

        return writeRoot;
    }



    private void ParseInner(
        Dictionary<int, object> stored,
        IWritingModule writer,
        ref IReadingModule reader,
        ref object readRoot,
        ref object writeRoot,
        ref object readHead,
        ref object writeHead,
        ref Type activeType,
        ref int i,
        int depth
        )
    {
        for (; i < _ops.Count; i++)
        {
            var o = _ops[i];

            var t = readHead?.GetType() ?? typeof(void);
            if (t != activeType)
            {
                activeType = t;
                reader = LookupModule(t);
            }

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
                    writer.Write(writeHead, o.StringAcc, readHead);
                    //write.Write(ref writeHead, o.IntAcc, readHead);
                    break;
                case ParseOperationType.ReadRoot:
                    readHead = readRoot;
                    break;
                case ParseOperationType.ReadAccess:
                    readHead = reader.Parse(readHead, o.StringAcc);
                    break;
                case ParseOperationType.ReadAccessInt:
                    readHead = reader.Parse(readHead, o.IntAcc);
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
                    writer.Append(writeHead, readHead);
                    break;
                case ParseOperationType.WriteForeach:
                    break;
                case ParseOperationType.ReadForeach:
                    i++;
                    var maxI = i;
                    var readResult = new List<object>();
                    foreach (var x in reader.Foreach(readHead))
                    {
                        var y = x;
                        var ii = i;
                        ParseInner(
                            stored,
                            writer,
                            ref reader,
                            ref readRoot,
                            ref writeRoot,
                            ref y,
                            ref writeHead,
                            ref activeType,
                            ref ii,
                            depth + 1
                        );
                        readResult.Add(y);
                        if (ii > maxI)
                            maxI = ii;
                    }
                    readHead = readResult;
                    i = maxI;
                    break;
                case ParseOperationType.UnbranchForeach:
                    return;
            }


        }
    }
}