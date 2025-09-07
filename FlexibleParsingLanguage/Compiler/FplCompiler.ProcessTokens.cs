using FlexibleParsingLanguage.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FlexibleParsingLanguage.Compiler;

public partial class FplCompiler
{
	private (SequenceProccessData, List<RawOp>) ProcessTokens(List<Token> tokens)
	{



		var data = new SequenceProccessData()
		{
			OpIdCounter = WriteRootId + 1,
        };

        var ops = new List<RawOp>(tokens.Count) {};
		bool checkedRoot = false;
		RawOp? op = null;
		var skipDefaultOperator = false;
		var it = tokens.GetEnumerator();

		for (var i = 0; i < tokens.Count; i++)
		{
			var name = string.Empty;
			var t = tokens[i];

			if (t.Op != null && t.Op.SequenceType.All(OpSequenceType.Named) && i + 1 < tokens.Count)
			{
				var t2 = tokens[i + 1];
				if (t2.Op == null)
				{
					i++;
					name = t2.Accessor;
				}
			}

			RawOp? accessor = null;
			if (t.Op == null || t.Op.SequenceType.All(OpSequenceType.Accessor))
			{
				accessor = new RawOp
				{
					Id = data.OpIdCounter++,
					CharIndex = t.Index,
					Type = t.Op ?? FplOperation.Accessor,
					Accessor = t.Accessor,
					FallbackAccessor = t.FallbackAccessor,
				};

				if (op != null && !op.Type.SequenceType.All(OpSequenceType.RightInput))
				{
					ops.Add(op);
					op = null;
				}

				if (op == null && !skipDefaultOperator)
				{
					op = new RawOp
					{
						Id = data.OpIdCounter++,
                        CharIndex = t.Index,
						Type = DefaultOp,
                        FallbackAccessor = t.FallbackAccessor,
                    };
				}
			}
			else
			{
				if (t.Op == DefaultOp)
				{
					skipDefaultOperator = false;
					continue;
				}


				if (op != null && op.Type != DefaultOp)
					ops.Add(op);

				op = new RawOp
				{
					Id = data.OpIdCounter++,
                    CharIndex = t.Index,
					Type = t.Op,
					Name = name,
                    FallbackAccessor = t.FallbackAccessor,
                };
			}

			if (!checkedRoot)
			{
				if (op.Type.SequenceType.All(OpSequenceType.LeftInput))
				{
                    data.RootOperatorId = data.OpIdCounter++;
                    ops.Add(new RawOp
                    {
                        Id = data.RootOperatorId,
                        CharIndex = t.Index,
                        Type = RootOperator,
                        FallbackAccessor = t.FallbackAccessor,
                    });
                }
                else
                {
					data.RootOperatorId = op.Id;
                }
            }

			checkedRoot = true;

			if (accessor != null)
			{
				if (op != null)
				{
					ops.Add(op);
					skipDefaultOperator = op.Type.SequenceType.All(OpSequenceType.OptionalExtraInput);
				}
				ops.Add(accessor);
				op = null;
				accessor = null;
			}
			else
			{
				skipDefaultOperator = false;
			}

			if (t.Op != null && t.Op.SequenceType.Any(OpSequenceType.GroupSeparator | OpSequenceType.Group))
				skipDefaultOperator = true;
		}

		if (op != null)
			ops.Add(op);

#if DEBUG
        var a = DebugSequence(ops);
#endif

        return (data, ops);
	}
}