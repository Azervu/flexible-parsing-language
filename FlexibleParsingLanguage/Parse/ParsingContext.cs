namespace FlexibleParsingLanguage.Parse;

internal partial class ParsingContext
{
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
                Reads = new List<ParsingFocusRead> { new ParsingFocusRead { Key = null, Read = ReadRoot, Config = parsingConfig } },
                MultiRead = false,
                Write = writeRoot,
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
                Reads = f.Reads,
                MultiRead = f.MultiRead,
                Write = root.Write,
            });
        }
        Focus = result;
    }
}