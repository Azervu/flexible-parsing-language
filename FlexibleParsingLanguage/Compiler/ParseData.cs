using FlexibleParsingLanguage.Parse;

namespace FlexibleParsingLanguage.Compiler;

internal class ParseData
{
    internal Dictionary<string, ITransformerFunction> Converter { get; set; }
    internal Dictionary<string, IFilterFunction> Filters { get; set; }
    internal int[] LoadedId { get; set; }
    internal Dictionary<int, RawOp> ProccessedMetaData { get; set; } = new Dictionary<int, RawOp>();
    internal Dictionary<int, int> LoadRedirect { get; set; } = new Dictionary<int, int>();
    internal Dictionary<int, (int ContextChangeId, OpCompileType Type)> WriteOutput { get; set; } = new();
}