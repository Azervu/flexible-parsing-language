using FlexibleParsingLanguage.Compiler;
using FlexibleParsingLanguage.Parse;
using System;
using System.Security.Cryptography;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FlexibleParsingLanguage.Operations;
internal static partial class FplOperation
{
    internal static readonly OpConfig Write = new OpConfig(":", OpSequenceType.RightInput | OpSequenceType.LeftInput)
    {
        Sequence = SequenceWrite,
        Compile = CompileWrite,
        CompileType = OpCompileType.WriteFlexible,
    };

    internal static readonly OpConfig WriteForeach = new OpConfig(":*", OpSequenceType.LeftInput)
    {
        Sequence = SequenceWrite,
        Compile = CompileWriteArray,
        CompileType = OpCompileType.WriteArray,
    };

    private static void SequenceWrite(SequenceProccessData d, RawOp o)
    {
        d.ActiveWriteId = o.Id;
    }

    private static IEnumerable<ParseOperation> CompileWrite(ParseData parser, RawOp op)
    {
        if (op.Input.Count < 1)
            throw new QueryException(op, $"{op.Input.Count} params | write takes at least 1");

        var id = op.GetStatusId(parser);
        var inputReadId = op.Input[0].ReadId;
        var inputWriteId = op.Input[0].WriteId;

        RawOp? accessor = null;

        if (op.Input.Count > 1)
            accessor = op.Input[1];

#if DEBUG

        if (accessor?.Accessor == "v")
        {
            var s = 456456;
        }
#endif
        if (op.Output.Count > 0)
        {
            var (writeId, writeType) = parser.WriteOutput[op.Output[0].Id];

            foreach (var x in FplOperation.CompileLoad(parser, op))
                yield return x;

            switch (writeType)
            {
                case OpCompileType.WriteObject:
                    yield return new ParseOperation(op, OperationWrite, new ParseOperationData() { StringAcc = accessor.Accessor, ReadId = inputReadId, WriteId = inputWriteId });
                    break;
                case OpCompileType.WriteArray:
                    yield return new ParseOperation(op, OperationWriteArray, new ParseOperationData() { StringAcc = accessor.Accessor, ReadId = inputReadId, WriteId = inputWriteId });
                    break;
            }
            op.WriteId = op.Id;
        }
        else if (accessor?.Type.SequenceType.All(OpSequenceType.Literal) != true && string.IsNullOrWhiteSpace(accessor?.Accessor))
        {
            yield return new ParseOperation(op, WriteAddRead, new ParseOperationData() { ReadId = inputReadId, WriteId = inputWriteId });
        }
        else if (accessor?.Type.SequenceType.All(OpSequenceType.Literal) == true || accessor == null || !int.TryParse(accessor.Accessor, out var intAcc))
        {
            yield return new ParseOperation(op, WriteFromRead, new ParseOperationData() { StringAcc = accessor.Accessor, ReadId = inputReadId, WriteId = inputWriteId });
        }
        else
        {
            throw new Exception("Write int access not yet supported");
        }

        parser.ActiveWriteId = op.Id;

        parser.LoadedId[0] = op.Id;

        foreach (var x in FplOperation.CompileSaved(parser, op))
            yield return x;
    }

    internal static void WriteAddRead(FplQuery parser, ParsingContext context, ParseOperationData d)
    {
        context.Focus.WriteFromRead(
            d,
            (x) => {
                return context.TransformReadInner(x.Value);
            },
            (w) =>
            {
                foreach (var r in w.Read)
                    context.WritingModule.Append(w.Write.V, r.V);
            }
        );
    }

    internal static void WriteFromRead(this ParsingFocusData focus, ParseOperationData d, Func<FocusEntry, ValueWrapper> extractRead, Action<WriteParam> action)
    {

        var ws = focus.GenerateSequencesIntersectionWriteRead(d.WriteId, d.ReadId);
        foreach (var rw in ws)
        {
            var write = rw.Primary;
            var read = rw.AVal;

            if (read.Foci.Count == 0)
                throw new Exception("no reads in sequence");
            var p = new WriteParam(read.Foci.Select(extractRead).ToList(), rw.Primary.Value, read.Multiread);
            action(p);
        }

        //List<FocusEntry> result = new List<FocusEntry>(); //TODO implment as store
        var opId = d.Id;
        var active = focus.Active;
        focus.Store[opId] = new ParsingNode(active.ReadId, opId, active.ConfigId);
        focus.Active = focus.Store[opId];
    }

    internal static void WriteFromRead(FplQuery parser, ParsingContext context, ParseOperationData d)
    {
        var acc = d.StringAcc;
        context.Focus.WriteFromRead(d, x => context.TransformReadInner(x.Value), (param) => {
            if (param.MultiRead)
                context.WritingModule.Write(param.Write.V, acc, param.Read.Select(x => x.V).ToList());
            else
                context.WritingModule.Write(param.Write.V, acc, param.Read[0].V);
        });
    }

    internal static void OperationWrite(FplQuery parser, ParsingContext context, ParseOperationData d)
    {
        var inputWriteId = (int)d.WriteId;
        context.WriteAction(d.Id, inputWriteId, (m, writeHeader) =>
        {
            var w = m.BlankMap();
            m.Write(writeHeader.V, d.StringAcc, w);
            return new ValueWrapper(w);
        });
    }

    internal static void OperationWriteArray(FplQuery parser, ParsingContext context, ParseOperationData d)
    {
        var inputWriteId = (int)d.WriteId;
        context.WriteAction(d.Id, inputWriteId, (m, writeHeader) =>
        {
            var w2 = m.BlankArray();
            m.Write(writeHeader.V, d.StringAcc, w2);
            return new ValueWrapper(w2);
        });
    }

    private static IEnumerable<ParseOperation> CompileWriteArray(ParseData parser, RawOp op)
    {
        if (op.Input.Count != 1)
            throw new QueryException(op, $"{op.Input.Count} write array | read takes 1");

        var inputReadId = op.Input[0].ReadId;
        var inputWriteId = op.Input[0].WriteId;

        var input = op.Input[0];
        var id = op.GetStatusId(parser);


        var (writeId, writeType) = parser.WriteOutput[op.Output[0].Id];

        foreach (var x in FplOperation.CompileLoad(parser, op))
            yield return x;

        switch (writeType)
        {
            case OpCompileType.WriteObject:
                yield return new ParseOperation(op, WriteFlattenObject, new ParseOperationData() { ReadId = inputReadId, WriteId = inputWriteId });
                break;
            case OpCompileType.WriteArray:
                yield return new ParseOperation(op, WriteFlattenArray, new ParseOperationData() { ReadId = inputReadId, WriteId = inputWriteId });
                break;
        }

        parser.LoadedId[0] = op.Id;

        parser.ActiveWriteId = op.Id;
        op.WriteId = op.Id;

        foreach (var x in FplOperation.CompileSaved(parser, op))
            yield return x;
    }

    internal static void WriteAction(this ParsingContext context, int opId, int writeTarget, Func<IWritingModule, ValueWrapper, ValueWrapper> writeFunc)
    {
        var focus = context.Focus;
        var writingModule = context.WritingModule;
        var active = focus.Active;
        var result = focus.Writes[active.WriteId].Select((x) => new FocusEntry { Value = writeFunc(writingModule, x.Value), SequenceId = x.SequenceId }).ToList();

        focus.Store[opId] = new ParsingNode(active.ReadId, opId, active.ConfigId, result);
        focus.Writes[opId] = result;
        focus.Active = focus.Store[opId];
    }

    internal static void WriteFlattenObject(FplQuery parser, ParsingContext context, ParseOperationData d)
    {
        context.Focus.WriteFlatten(d.Id, d.WriteId, d.ReadId, (writeParent) =>
        {
            var w = context.WritingModule.BlankMap();
            context.WritingModule.Append(writeParent.V, w);
            return new ValueWrapper(w);
        });
    }

    internal static void WriteFlattenArray(FplQuery parser, ParsingContext context, ParseOperationData d)
    {
        context.Focus.WriteFlatten(d.Id, d.WriteId, d.ReadId, (writeParent) =>
        {
            var w = context.WritingModule.BlankArray();
            context.WritingModule.Append(writeParent.V, w);
            return new ValueWrapper(w);
        });
    }

    internal static void WriteFlatten(this ParsingFocusData data, int opId, int writeTarget, int readTarget, Func<ValueWrapper, ValueWrapper> writeTransform)
    {
        data.NextWrite(opId,
            data.GenerateSequencesIntersectionWriteRead(writeTarget, readTarget)
            .SelectMany(x => x.AVal.Foci.Select(r => new FocusEntry { SequenceId = r.SequenceId, Value = writeTransform(x.Primary.Value) }))
            .ToList()
        );
    }
}