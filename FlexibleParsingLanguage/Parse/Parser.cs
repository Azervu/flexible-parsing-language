using FlexibleParsingLanguage.Converter;
using FlexibleParsingLanguage.Modules;

namespace FlexibleParsingLanguage.Parse;

public class ParserConfig
{
    public bool WriteArrayRoot { get; set; }
}

public class Parser
{
    private List<ParseOperation> _ops;
    private ModuleHandler _modules;
    private ParserConfig _parserConfig;
    private ParsingConfigContext _rootConfigContext;
    internal Dictionary<string, IConverter> _converter;

    internal Parser(List<ParseOperation> ops, ParserConfig parserConfig, ParsingConfigContext rootConfigContext)
    {
        _parserConfig = parserConfig;
        _rootConfigContext = rootConfigContext;
        _ops = ops;
        _modules = new ModuleHandler([
            new CollectionParsingModule(),
            new JsonParsingModule(),
            new XmlParsingModule(),

        ]);

        _converter = new Dictionary<string, IConverter>
        {
            { "json", new JsonConverter() },
            { "xml", new XmlConverter() }
        };
    }

    public object Parse(object readRoot)
    {
        IWritingModule writer = new CollectionWritingModule();
        var ctx = new ParsingContext(
            writer,
            _modules,
            readRoot,
            _parserConfig.WriteArrayRoot == true ? writer.BlankArray() : writer.BlankMap(),
           _rootConfigContext
        );


#if DEBUG
        var ops = _ops.Select(x => $"\n    {x.OpType} {x.StringAcc}").Concat();


        var s = 456654;
#endif



        foreach (var o in _ops)
            o.AppyOperation(this, ctx);
        return ctx.WriteRoot;
    }
}