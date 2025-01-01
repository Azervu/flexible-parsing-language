namespace FlexibleParsingLanguage.Parse;





internal class ParsingFocus
{

    internal List<ParsingFocusEntry> Entries;


    internal ParsingFocus(ParsingMetaContext parsingConfig, object readRoot, object writeRoot)
    {
        Entries = new List<ParsingFocusEntry> {
            new ParsingFocusEntry
            {
                Reads = new List<ParsingFocusRead> { new ParsingFocusRead { Key = null, Read = readRoot, Config = parsingConfig } },
                MultiRead = false,
                Write = writeRoot,
            }
        };
    }

    internal ParsingFocus(List<ParsingFocusEntry> entries)
    {
        Entries = entries;
    }




    internal ParsingFocus Transform(Func<ParsingFocusEntry, ParsingFocusEntry> transformAction)
    {
        var result = new List<ParsingFocusEntry>();
        foreach (var focusEntry in Entries)
            result.Add(transformAction(focusEntry));
        return new ParsingFocus(result);
    }







}



internal class ParsingFocusRead
{
    internal ParsingMetaContext Config;
    internal object Key;
    internal object Read;
    internal object Param;
}

internal struct ParsingFocusEntry
{
    internal List<ParsingFocusRead> Reads;
    internal bool MultiRead;
    internal object Write;
}