using FlexibleParsingLanguage.Operations;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Compiler;

internal partial class Compiler
{
    internal FplQuery CompileOperations(List<RawOp> ops, ParsingMetaContext configContext)
    {
        var parseData = new ParseData
        {
            ActiveId = RootId,
            LoadedId = RootId,
            IdCounter = RootId + 1,
            Ops = [],
            SaveOps = [],
            OpsMap = new Dictionary<(int LastOp, ParseOperation[]), int>
            {
            },
        };

        OpCompileType rootType = OpCompileType.None;


        var compiles = new List<ParseOperation>();


        foreach (var op in ops)
        {
            if (op.Type.Compile == null)
                continue;

            if (rootType == OpCompileType.None)
                rootType = op.Type.CompileType;

            foreach (var o in op.Type.Compile(parseData, op))
                compiles.Add(o);
        }


        if (rootType == OpCompileType.None)
            rootType = OpCompileType.WriteArray;

        return new FplQuery(compiles, configContext, new ParserRootConfig { RootType = rootType });
    }

}
