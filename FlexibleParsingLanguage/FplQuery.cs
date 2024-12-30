using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Converter;
using FlexibleParsingLanguage.Modules;
using FlexibleParsingLanguage.Operations;
using FlexibleParsingLanguage.Parse;
namespace FlexibleParsingLanguage;

public class FplQuery
{
    private static Compiler.Compiler _compiler { get; set; }
    internal static Compiler.Compiler Compiler {
        get
        {
            if (_compiler == null)
            {
                _compiler = new Compiler.Compiler([
                    FplOperation.Branch,
                    FplOperation.Read,
                    FplOperation.Write,
                    new OpConfig(",", OpSequenceType.GroupSeparator),
                    new OpConfig("(", OpSequenceType.Group | OpSequenceType.Virtual | OpSequenceType.Accessor, null, 100, ")"),
                    FplOperation.Param,
                    new OpConfig("~", OpSequenceType.LeftInput),
                    new OpConfig("*", OpSequenceType.LeftInput),

                    new OpConfig("|", OpSequenceType.RightInput | OpSequenceType.LeftInput),
                    new OpConfig("@", OpSequenceType.ParentInput | OpSequenceType.Virtual),
                    new OpConfig("#", OpSequenceType.RightInput | OpSequenceType.LeftInput),
                    new OpConfig("##", OpSequenceType.RightInput | OpSequenceType.LeftInput),
                    new OpConfig("\"", OpSequenceType.Literal, null, -1, "\""),
                    new OpConfig("'", OpSequenceType.Literal, null, -1, "\'"),
                    new OpConfig("\\", OpSequenceType.Unescape, null, -1)
                ]);
            }
            return _compiler;
        }
    }

    private List<ParseOperation> _operations;
    private ModuleHandler _modules;
    private ParsingMetaContext _rootMetaContext;
    private ParserRootConfig _config;
    internal Dictionary<string, IConverter> _converter;

    internal FplQuery(
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


    public static FplQuery Compile(string raw, ParsingMetaContext? configContext = null) => Compiler.Compile(raw, configContext);

    public object Parse(object readRoot)
    {

        IWritingModule writer = new CollectionWritingModule();

        object? writeRoot = null;
        switch (_config.RootType)
        {
            case OpCompileType.WriteArray:
                writeRoot = writer.BlankArray();
                break;
            case OpCompileType.WriteObject:
                writeRoot = writer.BlankMap();
                break;
        }

        var ctx = new ParsingContext(writer, _modules, readRoot, writeRoot, _rootMetaContext);

        foreach (var o in _operations)
        {
            try
            {
                o.Op(this, ctx, o.IntAcc, o.StringAcc);
            }
            catch (Exception ex)
            {
                throw new Exception($"Operation failed {o.OpType.GetMetaData().Name} | {ex.Message}", ex);
            }
        }

        return writeRoot;
    }
}