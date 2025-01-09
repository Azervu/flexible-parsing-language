using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Operations;

internal partial class FplOperation
{

    internal static readonly OpConfig Function = new OpConfig("|", OpSequenceType.LeftInput | OpSequenceType.RightInput, (p, o) => CompileSaveUtil(p, o, 2, [new ParseOperation(OperationFunction, o.Input[1].Accessor)]))
    {
        CompileType = OpCompileType.ReadArray,
    };


    internal static void OperationFunction(FplQuery parser, ParsingContext context, int intAcc, string acc)
    {

        if (parser._converter.TryGetValue(acc, out var c))
        {


            context.ReadTransformValue((w) =>
            {
                context.UpdateReadModule(new ValueWrapper(w));
                object raw;
                if (context.ReadingModule != null)
                    raw = context.ReadingModule.ExtractValue(w);
                else
                    raw = w;
                return c.Convert(raw);
            });
            return;
        }

        throw new NotImplementedException($"function does not exists {acc}");

        /*
        if (parser._filters.TryGetValue(acc, out var f))
        {
            context.Focus.ReadForeach((w) =>
            {
                context.UpdateReadModule(w);
                object raw;
                if (context.ReadingModule != null)
                    raw = context.ReadingModule.ExtractValue(w);
                else
                    raw = w.V;

                if (f.Filter())

                if (f.Convert(raw, out var result))
                    return new List<KeyValuePair<object, object>> {
                    new KeyValuePair<object, object>(w.V, result)
                };

                return new List<KeyValuePair<object, object>>();
            });
        }
        */

    }
}