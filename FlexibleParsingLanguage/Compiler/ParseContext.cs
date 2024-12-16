using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler;

internal class ParseContext
{

    internal AccessorData Accessor { get => Accessors[Index]; }

    internal AccessorData? NextAccessor { get => Index + 1 < Accessors.Count ? Accessors[Index + 1] : null; }

    internal bool LastWriteOp { get => (WriteMode == WriteMode.Write || WriteMode == WriteMode.Written) && (NextAccessor == null || NextAccessor.Operator == '}'); }

    internal List<AccessorData> Accessors = new List<AccessorData>();
    internal int ActiveId { get; set; }
    internal WriteMode WriteMode { get; set; } = WriteMode.Read;

    internal bool ProcessedEnd { get; set; }
    internal ParseContext Parent { get; set; }

    internal int Index { get; set; }


    internal AccessorData NextReadOperator()
    {
        for (var j = Index + 1; j < Accessors.Count; j++)
        {
            var op = Accessors[j];

            if (op.Ctx != null)
                return op.Ctx.FirstRead();

            return op;
        }
        return null;
    }

    internal AccessorData FirstRead()
    {
        var writes = false;
        foreach (var op in Accessors)
        {
            if (op.Ctx != null)
                return FirstRead();
            if (writes)
                return op;
            if (op.Operator == ':')
                writes = true;

        }
        return null;
    }

}
