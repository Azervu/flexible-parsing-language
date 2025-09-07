using FlexibleParsingLanguage.Converter;
using FlexibleParsingLanguage.Functions;
using FlexibleParsingLanguage.Modules;
using FlexibleParsingLanguage.Operations;
using FlexibleParsingLanguage.Parse;
using System.Text;

namespace FlexibleParsingLanguage.Compiler;
public partial class FplCompiler
{
    internal List<OpConfig> Ops { get; private set; }
    private OpConfig DefaultOp { get; set; }
    private OpConfig RootOperator { get; set; }
    private OpConfig Branch { get; set; }

    internal const int ReadRootId = 3;
    internal const int WriteRootId = 3;
    internal const int RootGroupId = 2;

    private string UnescapeToken { get; set; }

    private Dictionary<string, OpConfig?> Operators = new();


    private Dictionary<string, ITransformerFunction> _converter = new ();
    private Dictionary<string, IFilterFunction> _filters = new ();
    private ModuleHandler _modules;

    public void RegisterFilter(IFilterFunction filter)
    {
        _filters.Add(filter.Name, filter);
    }

    public void RegisterConverter(ITransformerFunction converter)
    {
        _converter.Add(converter.Name, converter);
    }

    public FplCompiler()
    {
        Ops = [
            FplOperation.Branch,
            FplOperation.Read,
            new OpConfig("[", OpSequenceType.LeftInput | OpSequenceType.Group | OpSequenceType.Virtual | OpSequenceType.Accessor, null, "]"),
            FplOperation.Foreach,
            FplOperation.Write,
            FplOperation.WriteForeach,
            FplOperation.Root,
            FplOperation.WriteRoot,
            FplOperation.Lookup,
            FplOperation.ChangeLookupContext,
            FplOperation.Function,
            FplOperation.SetVariable,
            FplOperation.AccessVariable,
            new OpConfig("~", OpSequenceType.LeftInput, (p, o) => FplOperation.CompileSaveUtil(p, o, 1, [new ParseOperation(o, ParsesOperationType.ReadName)])),
            new OpConfig("\"", OpSequenceType.Literal | OpSequenceType.Accessor, null, "\""),
            new OpConfig("'", OpSequenceType.Literal | OpSequenceType.Accessor, null, "\'"),
            FplOperation.Unescape,
            new OpConfig(",", OpSequenceType.GroupSeparator),
            new OpConfig("(", OpSequenceType.Group | OpSequenceType.Virtual | OpSequenceType.Accessor, null, ")"),
        ];

        DefaultOp = FplOperation.Read;
        RootOperator = FplOperation.Root;
        UnescapeToken = FplOperation.Unescape.Operator;
        Branch = FplOperation.Branch;

        _modules = new ModuleHandler([
            new FallbackModule(),
            new JsonParsingModule(),
            new XmlParsingModule(),
        ]);

        RegisterConverter(new XmlConverter());
        RegisterConverter(new JsonConverter());
        RegisterFilter(new RegexFilter());


        foreach (var op in Ops)
        {
            HandleConfigEntry(op.Operator, op);

            if (op.GroupOperator != null)
            {
                var op2 = op.GroupOperator.ToString();
                HandleConfigEntry(op2, new OpConfig(op2, OpSequenceType.UnGroup, null));
            }
        }
    }

    private void HandleConfigEntry(string op, OpConfig? config)
    {
        for (var i = 0; i < op.Length - 1; i++)
        {
            var o = op.Substring(i, i + 1);
            if (!Operators.ContainsKey(o))
                Operators[o] = null;
        }
        if (!Operators.TryGetValue(op, out var v) || v == null)
            Operators[op] = config;
    }

    internal FplQuery Compile(string raw, ParsingMetaContext? configContext = null)
    {
        var (ops, desugarizedQuery) = Lexicalize(raw);
        var compiled = CompileOperations(ops, configContext);
        compiled.RawQuery = raw;
        compiled.DesugaredQuery = desugarizedQuery;
        return compiled;
    }

    internal (List<RawOp>, string) Lexicalize(string raw)
    {
        string procsssedQuery = null;

        try
        {
            var tokens = Tokenize(raw).ToList();
            Desugarize(ref tokens);
            procsssedQuery = DesugarizedQuery(tokens);
            var (sequenceData, ops) = ProcessTokens(tokens);

            Sequence(sequenceData, ref ops);

#if DEBUG
            var debugLog = new StringBuilder();
            foreach (var o in ops)
            {
                if (o.Type.SequenceType.All(OpSequenceType.Accessor))
                    continue;

                debugLog.Append($"{o.Id}({o.Type.Operator}");
                if (o.Accessor != null)
                    debugLog.Append($"  {o.Accessor}");
                debugLog.Append(")");
                debugLog.Append($": ");

                if (o.LeftInput.Count() > 0)
                    debugLog.Append($"Left = [{o.LeftInput.Select(x => {
                        if (x.Type.SequenceType.All(OpSequenceType.Accessor))
                            return x.Accessor;
                        return x.Id.ToString();
                    }).Join(",")}] | ");

                if (o.RightInput.Count() > 0)
                    debugLog.Append($"Right = [{o.RightInput.Select(x => {
                        if (x.Type.SequenceType.All(OpSequenceType.Accessor))
                            return x.Accessor;
                        return x.Id.ToString();
                    }).Join(",")}] | ");

                if (o.GroupChildren.Count > 0)
                    debugLog.Append($"GroupChildren = [{o.GroupChildren.Select(x => $"[{x.Select(y => y.ToString()).Join(",")}]").Join(",")}] |");

                if (o.AffixChildren.Count > 0)
                    debugLog.Append($"AffixChildren = [{o.AffixChildren.Select(x => $"[{x.Select(y => y.ToString()).Join(",")}]").Join(",")}] |");

                debugLog.Append("\n");
            }
            var dl = debugLog.ToString();

#endif
            foreach (var op in ops)
            {
                if (op.Output.Count != 0)
                    op.Output.Clear();
            }
            foreach (var op in ops)
            {
                foreach (var o in op.Input)
                {
                    o.Output.Add(op);
                }
            }
            return (ops, procsssedQuery);
        }
        catch (QueryException ex)
        {
            throw new QueryException(ex.Ops, ex.RawMessage, ex.CompilerIssue)
            {
                RawMessage = ex.RawMessage,
                Query = procsssedQuery,
                RawQuery = raw
            };
        }
    }
}