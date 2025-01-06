using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Converter;
using FlexibleParsingLanguage.Modules;
using FlexibleParsingLanguage.Operations;
using FlexibleParsingLanguage.Parse;
using System.Reflection;
using System.Text;
namespace FlexibleParsingLanguage;

public class FplQuery
{
    private static Compiler.Compiler _compiler { get; set; }
    internal static Compiler.Compiler Compiler {
        get
        {
            if (_compiler == null)
                _compiler = new Compiler.Compiler(FplOperation.OpConfigs);
            return _compiler;
        }
    }

    private List<ParseOperation> _operations;
    private ModuleHandler _modules;
    private ParsingMetaContext _rootMetaContext;
    private ParserRootConfig _config;
    private IWritingModule? _writingModule;
    internal Dictionary<string, IConverter> _converter;

    private string _rawQuery;

    internal FplQuery(
        List<ParseOperation> operations,
        ParsingMetaContext rootMetaContext,
        ParserRootConfig config,
        string rawQuery
    )
    {
        _rawQuery = rawQuery;
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

    public static FplQuery Compile(string raw, ParsingMetaContext? configContext = null, IWritingModule? writingModule = null) {
        var c = Compiler.Compile(raw, configContext);
        c._writingModule = writingModule;
        return c;
    }

    public object Parse(object readRoot, IWritingModule? writingModule = null)
    {

        var writer = writingModule ?? _writingModule ?? new CollectionWritingModule();

        object? writeRoot = (_config.RootType & OpCompileType.WriteObject) > 0
            ? writer.BlankMap()
            : writer.BlankArray();
   

        var ctx = new ParsingContext(writer, _modules, readRoot, writeRoot, _rootMetaContext);

        foreach (var o in _operations)
        {
            try
            {
                o.Op(this, ctx, o.IntAcc, o.StringAcc);
            }
            catch (QueryException ex)
            {
                ex.Ops.Add(o.Metadata);
                ex.Query = _rawQuery;
                throw;
            } 
            catch (Exception ex)
            {

                string at = string.Empty;
                if (ex.StackTrace != null)
                {
                    var lines = ex.StackTrace.Split(Environment.NewLine);
                    for (var i = 0; i < lines.Length; i++)
                    {
                        if (!lines[i].Contains("FlexibleParsingLanguage"))
                            continue;
                        for (; i < lines.Length; i++)
                            at += "\n" + lines[i];
                        break;
                    }
                }

                var msg = new StringBuilder(ex.Message);
                msg.Append(" | version = ");
                msg.Append(Assembly.GetAssembly(typeof(ParsingContext)).GetName().Version.ToString());

                if (at != null)
                {
                    msg.Append(" | ");
                    msg.Append(at);
                }

                var ex2 = new QueryException(o.Metadata, msg.ToString(), true);
                ex2.Query = _rawQuery;

                throw ex2;
            }
        }

        return writeRoot;
    }
}