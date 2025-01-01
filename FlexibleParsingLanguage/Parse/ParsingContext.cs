namespace FlexibleParsingLanguage.Parse;

internal partial class ParsingContext
{
    internal ParsingMetaContext ConfigRoot;
    internal object ReadRoot;
    internal IReadingModule ReadingModule;
    internal object WriteRoot;
    internal IWritingModule WritingModule;
    internal readonly Dictionary<int, ParsingFocus> Store;
    internal ParsingFocus Focus;
    private Type _activeType = null;
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
        ReadRoot = readRoot;
        WriteRoot = writeRoot;
        Focus = new ParsingFocus(parsingConfig, readRoot, writeRoot ); 
        Store = new Dictionary<int, ParsingFocus> {
            { Compiler.Compiler.RootId, Focus }
        };
        WritingModule = writingModule;
        ConfigRoot = parsingConfig;
    }

    internal void MapFocus(Func<ParsingFocusEntry, ParsingFocusEntry> transformAction)
    {
        var result = new List<ParsingFocusEntry>();
        foreach (var focusEntry in Focus.Entries)
            result.Add(transformAction(focusEntry));
        Focus = new ParsingFocus(result);
    }

    internal void ToRootWrite()
    {
        var root = Store[Compiler.Compiler.RootId].Entries[0];
        var result = new List<ParsingFocusEntry>();
        foreach (var f in Focus.Entries)
        {
            result.Add(new ParsingFocusEntry
            {
                Reads = f.Reads,
                MultiRead = f.MultiRead,
                Write = root.Write,
            });
        }
        Focus = new ParsingFocus(result);
    }
}