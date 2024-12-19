using FlexibleParsingLanguage.Converter;
using FlexibleParsingLanguage.Modules;

namespace FlexibleParsingLanguage.Parse;

public class Parser
{
    private List<ParseOperation> _operations;
    private ModuleHandler _modules;
    private ParsingMetaContext _rootMetaContext;
    private ParserRootConfig _config;
    internal Dictionary<string, IConverter> _converter;

    internal Parser(
        List<ParseOperation> operations,
        ParsingMetaContext rootMetaContext,
        ParserRootConfig config
    )
    {
        _config = config;
        _rootMetaContext = rootMetaContext;
        _operations = operations;
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

#if DEBUG
        var ops = _operations.Select(x => $"\n    {x.OpType} {x.StringAcc} {x.IntAcc}").Concat();


        var s = 456654;
#endif

        IWritingModule writer = new CollectionWritingModule();

        object? writeRoot = null;
        switch (_config.RootType)
        {
            case WriteType.Array:
                writeRoot = writer.BlankArray();
                break;
            case WriteType.Object:
                writeRoot = writer.BlankMap();
                break;
        }

        var ctx = new ParsingContext(writer, _modules, readRoot, writeRoot, _rootMetaContext);

        foreach (var o in _operations)
            o.Op(this, ctx, o.IntAcc, o.StringAcc);

        return writeRoot;
    }
}