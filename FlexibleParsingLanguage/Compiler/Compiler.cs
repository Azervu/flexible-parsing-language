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

    internal const int RootId = 3;
    private OpConfig ParamOperator { get; set; }

    internal const int RootGroupId = 2;

    internal readonly OpConfig RootOperator;

    private string UnescapeToken { get; set; }

    private Dictionary<string, OpConfig?> Operators = new();


    private Dictionary<string, IConverterFunction> _converter = new Dictionary<string, IConverterFunction>();
    private Dictionary<string, IFilterFunction> _filters = new Dictionary<string, IFilterFunction>();
    private ModuleHandler _modules;

    public void RegisterFilter(IFilterFunction filter)
    {
        _filters.Add(filter.Name, filter);
    }

    public void RegisterConverter(IConverterFunction converter)
    {
        _converter.Add(converter.Name, converter);
    }


    public FplCompiler(List<OpConfig>? ops = null)
    {

        if (ops == null)
            ops = FplOperation.OpConfigs;

        _modules = new ModuleHandler([
            new CollectionParsingModule(),
            new JsonParsingModule(),
            new XmlParsingModule(),
        ]);

        RegisterConverter(new XmlConverter());
        RegisterConverter(new JsonConverter());
        RegisterFilter(new RegexFilter());


        Ops = ops;
        foreach (var op in ops)
        {
            HandleConfigEntry(op.Operator, op);

            if (op.GroupOperator != null)
            {
                var op2 = op.GroupOperator.ToString();
                HandleConfigEntry(op2, new OpConfig(op2, OpSequenceType.UnGroup, null));
            }

            if (op.SequenceType.All(OpSequenceType.Default))
                DefaultOp = op;

            if (op.SequenceType.All(OpSequenceType.RootParam))
                ParamOperator = op;

            if (op.SequenceType.All(OpSequenceType.Unescape))
                UnescapeToken = op.Operator;

            if (op.SequenceType.All(OpSequenceType.Root))
                RootOperator = op;
        }
        if (DefaultOp == null)
            throw new Exception("Default operator missing");

        if (ParamOperator == null)
            throw new Exception("Root operator missing");

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
        try
        {
            var ops = Lexicalize(raw);
            return CompileOperations(ops, configContext, raw);
        }
        catch (QueryException ex) {
            ex.Query = raw;
            throw;
        }
    }


    internal List<RawOp> Lexicalize(string raw)
    {

        try
        {
            var tokens = Tokenize(raw).ToList();
            var ops = ProcessTokens(tokens);

#if DEBUG
            var tt = tokens.Select(x => $"{x.Op?.Operator ?? ($"'{x.Accessor}'")}").Join("\n");
            var t = ops.Select(x => $"({x.Id,2}:{x.CharIndex,2}){(string.IsNullOrEmpty(x.Accessor) ? x.Type.Operator : $"'{x.Accessor}'"),5}").Join("\n");
#endif

            Sequence(ref ops);

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

            return ops;

        } catch (QueryException ex)
        {
            ex.Query = raw;
            throw;
        }
    }

    private List<RawOp> ProcessTokens(List<Token> tokens)
    {
        var ops = new List<RawOp>(tokens.Count) {
            new RawOp {
                Id = RootGroupId,
                CharIndex = 0,
                Type = RootOperator,
            },
        };

        var idCounter = RootId + 1;
        bool checkedRoot = false;

        RawOp? op = null;

        var skipDefaultOperator = false;


        var it = tokens.GetEnumerator();

        for (var i = 0; i < tokens.Count; i++)
        {
            var name = string.Empty;
            var t = tokens[i];

            if (t.Op != null && t.Op.SequenceType.All(OpSequenceType.Named) && i + 1 < tokens.Count)
            {
                var t2 = tokens[i + 1];
                if (t2.Op == null)
                {
                    i++;
                    name = t2.Accessor;
                }
            }

            RawOp? accessor = null;
            if (t.Op == null || t.Op.SequenceType.All(OpSequenceType.Accessor))
            {
                accessor = new RawOp
                {
                    Id = idCounter++,
                    CharIndex = t.Index,
                    Type = t.Op ?? FplOperation.Accessor,
                    Accessor = t.Accessor,
                };

                if (op != null && !op.Type.SequenceType.All(OpSequenceType.RightInput))
                {
                    ops.Add(op);
                    op = null;
                }

                if (op == null && !skipDefaultOperator)
                {
                    op = new RawOp
                    {
                        Id = idCounter++,
                        CharIndex = t.Index,
                        Type = DefaultOp,
                    };
                }
            }
            else
            {
                if (t.Op == DefaultOp)
                {
                    skipDefaultOperator = false;
                    continue;
                }


                if (op != null && op.Type != DefaultOp)
                    ops.Add(op);

                op = new RawOp
                {
                    Id = idCounter++,
                    CharIndex = t.Index,
                    Type = t.Op,
                    Name = name,
                };
            }


            if (!checkedRoot)
            {
                checkedRoot = true;
                if (op.Type.SequenceType.All(OpSequenceType.LeftInput))
                {
                    ops.Add(new RawOp
                    {
                        Id = idCounter++,
                        CharIndex = t.Index,
                        Type = ParamOperator,
                    });
                }
            }

            if (accessor != null)
            {
                if (op != null)
                {
                    ops.Add(op);
                    skipDefaultOperator = op.Type.SequenceType.All(OpSequenceType.OptionalExtraInput);
                }
                ops.Add(accessor);
                op = null;
                accessor = null;
            }
            else
            {
                skipDefaultOperator = false;
            }

            if (t.Op != null && t.Op.SequenceType.Any(OpSequenceType.GroupSeparator | OpSequenceType.Group))
                skipDefaultOperator = true;
        }



        /*

        while (it.MoveNext()) {
            var t = it.Current;



            //RawOp? acc = null;

            RawOp? accessor = null;
            if (t.Op == null || t.Op.SequenceType.All(OpSequenceType.Accessor))
            {
                accessor = new RawOp
                {
                    Id = idCounter++,
                    CharIndex = t.Index,
                    Type = t.Op ?? FplOperation.Accessor,
                    Accessor = t.Accessor,
                };

                if (op != null && !op.Type.SequenceType.Any(OpSequenceType.RightInput))
                {
                    ops.Add(op);
                    op = null;
                }

                if (op == null && !skipDefaultOperator)
                {
                    op = new RawOp
                    {
                        Id = idCounter++,
                        CharIndex = t.Index,
                        Type = DefaultOp,
                    };
                }
            }
            else
            {
                if (t.Op == DefaultOp)
                {
                    skipDefaultOperator = false;
                    continue;
                }
                 

                if (op != null && op.Type != DefaultOp)
                    ops.Add(op);

                op = new RawOp
                {
                    Id = idCounter++,
                    CharIndex = t.Index,
                    Type = t.Op,
                };
            }


            if (!checkedRoot)
            {
                checkedRoot = true;
                if (op.Type.SequenceType.All(OpSequenceType.LeftInput))
                {
                    ops.Add(new RawOp
                    {
                        Id = idCounter++,
                        CharIndex = t.Index,
                        Type = ParamOperator,
                    });
                }
            }


            if (accessor != null)
            {
                if (op != null)
                {
                    ops.Add(op);
                    skipDefaultOperator = op.Type.SequenceType.All(OpSequenceType.OptionalExtraInput);
                }
                ops.Add(accessor);
                op = null;
                accessor = null;
            }
            else
            {
                skipDefaultOperator = false;
            }

            if (t.Op != null && t.Op.SequenceType.Any(OpSequenceType.GroupSeparator | OpSequenceType.Group))
                skipDefaultOperator = true;

        }
        */

        if (op != null)
            ops.Add(op);

        if (RootOperator.SequenceType.All(OpSequenceType.Group))
        {
            ops.Add(new RawOp
            {
                Id = idCounter++,
                CharIndex = 0,
                Type = Operators[RootOperator.GroupOperator],
            });
        };

        return ops;
    }
}