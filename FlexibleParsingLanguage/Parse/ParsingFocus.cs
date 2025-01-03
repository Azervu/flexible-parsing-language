namespace FlexibleParsingLanguage.Parse;

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