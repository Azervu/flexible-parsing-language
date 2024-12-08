namespace FlexibleParsingLanguage.Parse;

internal partial class ParsingContext
{
    internal struct ParsingFocusEntry
    {
        internal ParsingConfigContext Config;
        internal List<object> Keys;
        internal List<object> Reads;
        internal bool MultiRead;
        internal object Write;
    }

    internal ParsingConfigContext ConfigRoot;
    internal object ReadRoot;
    internal IReadingModule ReadingModule;
    internal object WriteRoot;
    internal IWritingModule WritingModule;
    internal readonly Dictionary<int, List<ParsingFocusEntry>> Store;
    internal List<ParsingFocusEntry> Focus;
    private Type _activeType = null;
    private ModuleHandler _modules;

    public ParsingContext(
        IWritingModule writingModule,
        ModuleHandler modules,
        object readRoot,
        object writeRoot,
        ParsingConfigContext parsingConfig
    )
    {
        _modules = modules;
        ReadRoot = readRoot;
        WriteRoot = writeRoot;
        Focus = new List<ParsingFocusEntry> {
            new ParsingFocusEntry
            {
                Keys = new List<object> { null },
                Reads = new List<object> { readRoot },
                MultiRead = false,
                Write = writeRoot,
                Config = parsingConfig
            }
        };
        Store = new Dictionary<int, List<ParsingFocusEntry>> {
            { 1, Focus }
        };
        WritingModule = writingModule;
        ConfigRoot = parsingConfig;
    }

    internal void MapFocus(Func<ParsingFocusEntry, ParsingFocusEntry> transformAction)
    {
        var results = new List<ParsingFocusEntry>();
        foreach (var focusEntry in Focus)
            results.Add(transformAction(focusEntry));
        Focus = results;
    }

    internal void ToRootWrite()
    {
        var root = Store[1][0];
        var result = new List<ParsingFocusEntry>();
        foreach (var f in Focus)
        {
            result.Add(new ParsingFocusEntry
            {
                Keys = root.Keys,
                Reads = f.Reads,
                MultiRead = f.MultiRead,
                Write = root.Write,
                Config = f.Config,
            });
        }
        Focus = result;
    }


}