using FlexibleParsingLanguage.Parse;

namespace FlexibleParsingLanguage.Compiler;

internal class ParseData
{


    private int _activeReadId = -1;
    internal int ActiveReadId
    {
        get => _activeReadId;
        set
        {
#if DEBUG
            if (value == 18)
            {
                var s = 3456456;
            }
#endif
            _activeReadId = value;
        }
    }

    internal int ActiveWriteId { get; set; } = -1;

    internal Dictionary<string, ITransformerFunction> Converter { get; set; }
    internal Dictionary<string, IFilterFunction> Filters { get; set; }
    internal int[] LoadedId { get; set; }
    internal Dictionary<int, RawOp> ProccessedMetaData { get; set; } = new Dictionary<int, RawOp>();
    internal Dictionary<int, int> LoadRedirect { get; set; } = new Dictionary<int, int>();
    internal Dictionary<int, (int ContextChangeId, OpCompileType Type)> WriteOutput { get; set; } = new();
}