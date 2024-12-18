namespace FlexibleParsingLanguage.Parse
{
    internal struct ParsingFocusRead
    {
        internal ParsingMetaContext Config;
        internal object Key;
        internal object Read;
    }

    internal struct ParsingFocusEntry
    {
        internal List<ParsingFocusRead> Reads;
        internal bool MultiRead;
        internal object Write;
    }
}
