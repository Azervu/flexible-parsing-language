using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage;

internal enum ParseOperationType
{
    WriteInitRoot,
    WriteRoot,
    WriteAccess,
    WriteAccessInt,
    ReadRoot,
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





public interface IParsingModule
{
    public List<Type> HandledTypes { get; }
    public object Parse(object raw, string acc);
    public object Parse(object raw, int acc);
}

public interface IWritingModule
{
    public List<Type> HandledTypes { get; }
    public object Root();
    public void Write(ref object raw, string acc, object? val);
    public void Write(ref object raw, int acc, object? val);
}



public class Parser
{
    private List<ParseOperation> _ops;

    private List<(List<Type>, IParsingModule)> _modules;

    private Dictionary<Type, IParsingModule> _moduleLookup;

    private Type activeType;

    internal Parser(List<ParseOperation> ops)
    {
        _ops = ops;

        _modules = new List<(List<Type>, IParsingModule)>
        {
            ([typeof(JsonNode)] , new JsonParsingModule() )
        };
        _moduleLookup = new Dictionary<Type, IParsingModule>();
    }

    private IParsingModule LookupModule(Type t)
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
        IWritingModule write = new CollectionWritingModule();
        Type activeType = null;
        IParsingModule activeModule = null;

        object writeRoot = null;
        object readHead = readRoot;
        object writeHead = null;

        foreach (var o in _ops)
        {
            var t = readHead?.GetType() ?? typeof(void);
            if (t != activeType)
            {
                activeType = t;
                activeModule = LookupModule(t);
            }
            switch (o.OpType)
            {
                case ParseOperationType.WriteInitRoot:
                    writeRoot = write.Root();
                    writeHead = writeRoot;
                    break;
                case ParseOperationType.WriteRoot:
                    writeHead = writeRoot;
                    break;
                case ParseOperationType.WriteAccess:
                    write.Write(ref writeHead, o.StringAcc, readHead);
                    break;
                case ParseOperationType.WriteAccessInt:
                    write.Write(ref writeHead, o.IntAcc, readHead);
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
            }
        }
        return writeRoot;
    }
}