using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Converter;
using FlexibleParsingLanguage.Functions;
using FlexibleParsingLanguage.Modules;
using FlexibleParsingLanguage.Operations;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
namespace FlexibleParsingLanguage;

public class FplQuery
{
    private static Compiler.FplCompiler _compiler { get; set; }
    internal static Compiler.FplCompiler Compiler {
        get
        {
            if (_compiler == null)
                _compiler = new Compiler.FplCompiler();
            return _compiler;
        }
    }

    private List<ParseOperation> _operations;
    
    private ParsingMetaContext _rootMetaContext;
    private IWritingModule? _writingModule;
    private ModuleHandler _modules;
    internal string DesugaredQuery { get; set; }
    internal string RawQuery { get; set; }
    internal FplQuery(
        List<ParseOperation> operations,
        ParsingMetaContext rootMetaContext,
        ModuleHandler modules
    )
    {
        _rootMetaContext = rootMetaContext;
        _operations = operations;
        _modules = modules;
    }

    public static FplQuery Compile(string raw, ParsingMetaContext? configContext = null, IWritingModule? writingModule = null) {
        var c = Compiler.Compile(raw, configContext);
        c._writingModule = writingModule;
        return c;
    }

    public object Parse(object readRoot, IWritingModule? writingModule = null)
    {
        var writer = writingModule ?? _writingModule ?? new CollectionWritingModule();

        var writeRoot = new WriteRoot();

        var ctx = new ParsingContext(writer, _modules, readRoot, writeRoot, _rootMetaContext);


#if DEBUG
        RawOp activeOp = null;
        var log = string.Empty;
#endif


        ParseOperation o = null;
        try
        {


            foreach (var x in _operations)
            {
                o = x;
                o.Op(this, ctx, o.Data);
                ctx.Focus.Store[o.Data.Id] = ctx.Focus.Active;
#if DEBUG
                ctx.Focus.ValidateTree();
#endif
            }
        }

        catch (QueryException ex)
        {
            ex.Ops.Add(o.Metadata);
            ex.Query = DesugaredQuery;
            ex.RawQuery = RawQuery;
            throw ex;
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
            ex2.Query = DesugaredQuery;
            ex2.RawQuery = RawQuery;
            throw ex2;
        }

        if (writeRoot.Dictionary.Count() == 0)
            return writeRoot.List;
        if (writeRoot.List.Count == 0)
            return writeRoot.Dictionary;

        var dict = writeRoot.Dictionary.ToDictionary();
        for(var i = 0; i < writeRoot.List.Count; i++)
            dict[i.ToString()] = writeRoot.List[i];
        return dict;
    }
}