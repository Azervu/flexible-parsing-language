using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Parse;

internal class ParsingNode
{
    internal int WriteId { get; private set; }
    internal int ReadId { get; private set; }
    internal int ConfigId { get; private set; }

    internal List<FocusEntry> Data { get; private set; }

    internal ParsingNode(int readId, int writeId, int configId, List<FocusEntry> data = null)
    {
#if DEBUG
        if (readId == 0 && writeId == 0 && configId == 0)
            throw new Exception($"read = {readId} | write = {writeId} | config = {configId}");
#endif


        WriteId = writeId;
        ReadId = readId;
        ConfigId = configId;


        Data = data;
    }
}