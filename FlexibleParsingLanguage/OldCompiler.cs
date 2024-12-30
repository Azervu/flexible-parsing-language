using FlexibleParsingLanguage.Parse;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace FlexibleParsingLanguage.Compiler;

internal class ParseData
{
    internal List<(int, ParseOperation)> Ops { get; set; }
    internal Dictionary<(int LastOp, ParseOperation[]), int> OpsMap { get; set; }
    internal HashSet<int> SaveOps { get; set; } = new HashSet<int>();
    internal int IdCounter { get; set; }
    internal int LoadedId { get; set; }
    internal int ActiveId { get; set; }

    internal Dictionary<int, RawOp> ProccessedMetaData { get; set; } = new Dictionary<int, RawOp>();

    internal Dictionary<int, int> LoadRedirect { get; set; } = new Dictionary<int, int>();

}

internal partial class OldCompiler
{

    public const int ROOT_ID = 1;

    internal Compiler Compiler { get; private set; }
}