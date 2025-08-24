namespace FlexibleParsingLanguage.Parse;

internal class ParsingContext
{
    internal IWritingModule WritingModule;
    internal ParsingFocusData Focus;
    private ModuleHandler _modules;

    public ParsingContext(
        IWritingModule writingModule,
        ModuleHandler modules,
        object readRoot,
        object writeRoot,
        ParsingMetaContext parsingConfig
    )
    {
        _modules = modules;
        Focus = new ParsingFocusData(parsingConfig, readRoot, writeRoot);
        WritingModule = writingModule;
    }


    internal IReadingModule GetReadingModule(ValueWrapper obj) => _modules.LookupModule(obj.V?.GetType() ?? typeof(void));

    internal IReadingModule GetReadingModule(object obj) => _modules.LookupModule(obj?.GetType() ?? typeof(void));

    private object ExtractReadValue(FocusEntry w) => GetReadingModule(w.Value).ExtractValue(w.Value.V);

    internal ValueWrapper TransformReadInner(ValueWrapper raw) => new  ValueWrapper(GetReadingModule(raw).ExtractValue(raw.V));

    internal void WriteFlatten(int opId)
    {
        Focus.WriteFlatten(opId, (writeParent) =>
        {
            var w = WritingModule.BlankMap();
            WritingModule.Append(writeParent.V, w);
            return new ValueWrapper(w);
        });
    }

    internal void WriteFlattenArray(int opId)
    {
        Focus.WriteFlatten(opId, (writeParent) =>
        {
            var w = WritingModule.BlankArray();
            WritingModule.Append(writeParent.V, w);
            return new ValueWrapper(w);
        });
    }

    internal void WriteStringFromRead(string acc)
    {
        WriteFromRead(x => TransformReadInner(x.Value), (param) => {
            if (param.MultiRead)
                WritingModule.Write(param.Write.V, acc, param.Read.Select(x => x.V).ToList());
            else
                WritingModule.Write(param.Write.V, acc, param.Read[0].V);
        });
    }

    internal void WriteFromRead(Func<FocusEntry, ValueWrapper> readFunc, Action<WriteParam> writeAction) => Focus.WriteFromRead(readFunc, writeAction);

    internal static void WriteAddRead(FplQuery parser, ParsingContext context, ParseOperationData d)
    {
        context.WriteFromRead((x) => context.TransformReadInner(x.Value), (w) =>
        {
            foreach (var r in w.Read)
                context.WritingModule.Append(w.Write.V, r.V);
        });
    }

    internal void WriteAction(int opId, Func<IWritingModule, ValueWrapper, ValueWrapper> writeFunc) => Focus.Write(opId, (data) => writeFunc(WritingModule, data));

    internal IEnumerable<ParameterInfo> GetActiveParameters(List<CompiledParameter> parameters)
    {
        var primaryFocus = Focus.Reads[Focus.Active.ReadId];

        var primarySequences = primaryFocus
            .Select(x => x.SequenceId)
            .ToList();

        var secondarySequences = new List<List<int>>();
        var secondaryFocuses = new List<(int, List<FocusEntry>)>();

        for (var i = 0; i < parameters.Count; i++)
        {
            var p = parameters[i];
            if (p.IsLiteral)
                continue;

            var node = Focus.Store[p.Id];
            var secFocus = Focus.Reads[node.ReadId];
            secondarySequences.Add(secFocus.Select(x => x.SequenceId).ToList());
            secondaryFocuses.Add((i, secFocus));
        }

        var sequenceIntersection = Focus.GenerateSequencesIntersectionInner(primarySequences, secondarySequences.ToArray());
        for (var i = 0; i < sequenceIntersection.Count(); i++)
        {
            var (sequenceId, sequenceParameters) = sequenceIntersection[i];

            if (sequenceId == -1)
                throw new Exception("SequenceId parent = -1");

            var secondaryData = parameters.Select(x => new SecondaryParamInfo { CompiledParameter = x, Value = x.Accessor }).ToArray();

            for (var dynamicParamIndex = 0; dynamicParamIndex < sequenceParameters.Length; dynamicParamIndex++)
            {
                var sp = sequenceParameters[dynamicParamIndex];
                var (j, secondaryFocusEntries) = secondaryFocuses[dynamicParamIndex];
                var value = sp.Multiread
                    ? sp.Foci.Select(k => ExtractReadValue(secondaryFocusEntries[k.Index])).ToList()
                    : ExtractReadValue(secondaryFocusEntries[sp.Foci[0].Index]);

                secondaryData[j].Value = value;
            }

            yield return new ParameterInfo
            {
                Primary = ExtractReadValue(primaryFocus[i]),
                SequenceId = sequenceId,
                Secondary = secondaryData
            };
        }
    }
}