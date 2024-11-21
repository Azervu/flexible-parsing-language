using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage;

internal enum ParseOperationType
{
    WriteInitRoot,
    WriteRoot,
    WriteLoad,
    WriteAccess,
    WriteAccessInt,
    WriteFromRead,
    ReadRoot,
    ReadLoad,
    ReadAccess,
    ReadAccessInt,
}

internal struct ParseOperation
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
    public object Root();
    public void Write(object raw, string acc, object? val);
    public void Write(object raw, int acc, object? val);
}



public class Parser
{
    private List<ParseOperation> _ops;

    private List<(List<Type>, IReadingModule)> _modules;

    private Dictionary<Type, IReadingModule> _moduleLookup;

    private Type activeType;

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

        object writeRoot = null;
        object readHead = readRoot;
        object writeHead = null;

        Type activeType = null;

        foreach (var o in _ops)
        {
            var t = readHead?.GetType() ?? typeof(void);
            if (t != activeType)
            {
                activeType = t;
                reader = LookupModule(t);
            }
            ParseInner(
                writer,
                reader,
                ref readRoot,
                ref writeRoot,
                ref readHead,
                ref writeHead,
                o
            );
        }
        return writeRoot;
    }


    private void ParseInner(
        IWritingModule write,
        IReadingModule activeModule,
        ref object readRoot,
        ref object writeRoot,
        ref object readHead,
        ref object writeHead,
        ParseOperation o
        )
    {
        switch (o.OpType)
        {
            case ParseOperationType.WriteInitRoot:
                writeRoot = write.Root();
                writeHead = writeRoot;
                break;
            case ParseOperationType.WriteLoad:
                break;
            case ParseOperationType.WriteRoot:
                writeHead = writeRoot;
                break;
            case ParseOperationType.WriteAccessInt:
            case ParseOperationType.WriteAccess:
                var w = write.Root();
                write.Write(writeHead, o.StringAcc, w);
                writeHead = w;
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
            case ParseOperationType.ReadLoad:
                break;


        }
    }


}