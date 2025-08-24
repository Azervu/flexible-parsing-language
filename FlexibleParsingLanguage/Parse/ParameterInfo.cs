using FlexibleParsingLanguage.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Parse;
internal struct ParameterInfo
{
    internal int SequenceId { get; set; }
    internal object Primary { get; set; }
    internal SecondaryParamInfo[] Secondary { get; set; }
}

internal struct SecondaryParamInfo
{
    internal CompiledParameter CompiledParameter { get; set; }
    internal object Value { get; set; }
}

internal struct CompiledParameter
{
    internal bool IsLiteral { get; set; }
    internal int Id { get; set; }
    internal string Accessor { get; set; }

    internal CompiledParameter(bool isLiteral, int id, string accessor)
    {
        IsLiteral = isLiteral;
        Id = id;
        Accessor = accessor;
    }

    internal static List<CompiledParameter> CompileParameters(ParseData parser, RawOp op, int startIndex = 2)
    {
        var parameters = new List<CompiledParameter>();
        for (var i = startIndex; i < op.Input.Count; i++)
        {
            var x = op.Input[i];
            if ((x.Type.SequenceType & (OpSequenceType.Literal | OpSequenceType.Accessor)) > 0)
                parameters.Add(new CompiledParameter(true, x.Id, x.Accessor));
            else if (x.Type.GetStatusId == null)
                parameters.Add(new CompiledParameter(false, x.Id, x.Accessor));
            else
                parameters.Add(new CompiledParameter(false, x.Type.GetStatusId(parser, x), x.Accessor));
        }
        return parameters;
    }
}
