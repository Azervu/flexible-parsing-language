using FlexibleParsingLanguage.Parse;

namespace FlexibleParsingLanguage.Compiler;

internal class ParseData
{
    internal Dictionary<string, IConverterFunction> Converter { get; set; }
    internal Dictionary<string, IFilterFunction_String> Filters { get; set; }

    internal int LoadedId { get; set; }
    internal int ActiveId { get; set; }

    internal Dictionary<int, RawOp> ProccessedMetaData { get; set; } = new Dictionary<int, RawOp>();
    internal Dictionary<int, int> LoadRedirect { get; set; } = new Dictionary<int, int>();
    internal Dictionary<int, (int ContextChangeId, OpCompileType Type)> ReadInput { get; set; } = new();
    internal Dictionary<int, (int ContextChangeId, OpCompileType Type)> WriteInput { get; set; } = new();
    internal Dictionary<int, (int ContextChangeId, OpCompileType Type)> ReadOutput { get; set; } = new();
    internal Dictionary<int, (int ContextChangeId, OpCompileType Type)> WriteOutput { get; set; } = new();
}