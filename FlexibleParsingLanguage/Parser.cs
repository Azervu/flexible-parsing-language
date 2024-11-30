using FlexibleParsingLanguage.Modules;
using System.Collections;

namespace FlexibleParsingLanguage;

internal enum ParseOperationType
{
    Root,
    Save,
    Load,
 
    WriteAccess,
    WriteAccessInt,
    WriteFromRead,
    WriteForeachArray,
    AddFromRead,
    
    ReadAccess,
    ReadAccessInt,
    ReadForeach,
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

public class ParserConfig
{
    public bool? WriteArrayRoot { get; set; }
}

public class Parser
{
    private List<ParseOperation> _ops;
    private ModuleHandler _modules;
    private ParserConfig _parserConfig;

    internal Parser(List<ParseOperation> ops, ParserConfig parserConfig)
    {
        _parserConfig = parserConfig;
        _ops = ops;
        _modules = new ModuleHandler([
            new JsonParsingModule(),
            new CollectionParsingModule()
        ]);
    }

    public object Parse(object readRoot)
    {
        IWritingModule writer = new CollectionWritingModule();

        var ctx = new ParsingContext(
            writer,
            _modules,
            readRoot,
            _parserConfig.WriteArrayRoot == true ? writer.BlankArray() : writer.BlankMap()
        );

        foreach (var o in _ops)
            o.AppyOperation(ctx);

        return ctx.WriteRoot;
    }
}