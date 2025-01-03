using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Parse;

internal class ParsingFocusData
{

    private int _sequenceIdCounter = 1;
    private int _readIdCounter = 1;
    private int _writeIdCounter = 1;


    internal Dictionary<int, ParsingFocus2> Store { get; set; }

    internal Dictionary<int, ParsingSequence> Sequences { get; set; } // childId to parent id

    internal Dictionary<int, List<ReadFocusEntry>> Reads { get; set; }

    internal Dictionary<int, List<WriteFocusEntry>> Writes { get; set; }

    internal ParsingFocus2 Active { get; set; }


    internal ParsingFocusData(ParsingMetaContext parsingConfig, object readRoot, object writeRoot)
    {
        Sequences = new Dictionary<int, ParsingSequence> {
            { _sequenceIdCounter, new ParsingSequence { ParentId = -1 } }
        };

        Reads = new Dictionary<int, List<ReadFocusEntry>>
        {
            { _readIdCounter, [ new ReadFocusEntry { Value = new ValueWrapper(readRoot), SequenceId = _sequenceIdCounter } ] }
        };

        Writes = new Dictionary<int, List<WriteFocusEntry>>
        {
            { _writeIdCounter, [ new WriteFocusEntry { Value = new ValueWrapper(writeRoot), SequenceId = _sequenceIdCounter } ] }
        };

        Active = new ParsingFocus2
        {
            WriteId = _writeIdCounter,
            ReadId = _readIdCounter,
        };

        Store = new Dictionary<int, ParsingFocus2> {
            { Compiler.Compiler.RootId, Active }
        };
    }


    internal void Save(int id)
    {
        Store[id] = Active;
    }

    internal void Load(int id)
    {
        Active = Store[id];
    }

    internal void LoadRead(int id)
    {
        var readId = Store[id].ReadId;
        Active = new ParsingFocus2(readId, Active.WriteId);
    }

    internal void LoadWrite(int id)
    {
        var writeId = Store[id].WriteId;
        Active = new ParsingFocus2(Active.ReadId, writeId);
    }

    internal void Read(Func<ValueWrapper, KeyValuePair<ValueWrapper, ValueWrapper>> transform) => ReadInner(x =>
    {
        var kv = transform(x.Value);
        return new ReadFocusEntry { Key = kv.Key, Value = kv.Value, SequenceId = x.SequenceId, };
    });
    internal void ReadInner(Func<ReadFocusEntry, ReadFocusEntry> transform)
    {

        var raws = Reads[Active.ReadId];
        var reads = raws.Select(transform).ToList();

#if DEBUG
        if (reads.Count == 0)
            throw new Exception("ReadInner resulted in no reads");
#endif


        Reads[++_readIdCounter] = reads;
        Active = new ParsingFocus2(_readIdCounter, Active.WriteId);
    }

    internal void Write(Func<ValueWrapper, ValueWrapper> transform) => WriteInner((x) => new WriteFocusEntry { Value = transform(x.Value), SequenceId = x.SequenceId });

    internal void WriteInner(Func<WriteFocusEntry, WriteFocusEntry> transform)
    {
        var writes = Writes[Active.WriteId];
        Writes[++_writeIdCounter] = writes.Select(transform).ToList();
        Active = new ParsingFocus2(Active.ReadId, _writeIdCounter);
    }

    internal void WriteFromRead(Func<ReadFocusEntry, ValueWrapper> extractRead, Action<WriteParam> action) => WriteFromRead(Writes[Active.WriteId], Reads[Active.ReadId], extractRead, action);

    internal void WriteFromRead(List<WriteFocusEntry> writes, List<ReadFocusEntry> reads, Func<ReadFocusEntry, ValueWrapper> extractRead, Action<WriteParam> action)
    {
        var ws = GenerateSequencesIntersection(writes, reads);
        foreach (var rw in ws)
        {
            var w = rw.Value.Item1;
            var r = rw.Value.Item2;

            if (r.Count == 0)
                throw new Exception("no reads in sequence");
            var sameSequence = w.SequenceId == r[0].SequenceId;
            var readValues = r.Select(extractRead).ToList();

#if DEBUG 
            if (sameSequence && r.Count != 1)
                throw new Exception("multiple in same sequence");
#endif
            var p = new WriteParam(readValues, w.Value, !sameSequence);
            action(p);
        }
    }

    private new Dictionary<int, (WriteFocusEntry Write, List<ReadFocusEntry> Read)> GenerateSequencesIntersection(List<WriteFocusEntry> writes, List<ReadFocusEntry> reads)
    {
        var rwSequences = new Dictionary<int, (WriteFocusEntry Write, List<ReadFocusEntry> Read)>();

        foreach (var w in writes)
        {
            if (rwSequences.ContainsKey(w.SequenceId))
                throw new InvalidOperationException("multiple write heads on same sequence");
            rwSequences[w.SequenceId] = (w, new());
        }

        Dictionary<int, List<int>> writeChildSequences = new();
        foreach (var id in writes.Select(x => x.SequenceId))
        {
            var activeId = id;
            while (activeId >= 0)
            {
                if (!writeChildSequences.TryGetValue(activeId, out var values))
                {
                    values = [id];
                    writeChildSequences.Add(activeId, values);
                }
                else
                {
                    values.Add(id);
                }
                activeId = Sequences[activeId].ParentId;
            }
        }

        foreach (var sg in reads.GroupBy(x => x.SequenceId))
        {
            var activeId = sg.Key;
            List<int>? ws = null;
            while (!writeChildSequences.TryGetValue(activeId, out ws))
            {
                activeId = Sequences[activeId].ParentId;
                if (activeId < 0)
                    throw new InvalidOperationException($"read/write missing shared root | writes = [{writeChildSequences.Select(x => x.Key.ToString()).Join(", ")}] | reads = [{reads.GroupBy(x => x.SequenceId).Select(x => x.Key.ToString()).Join(", ")}]");
            }
            foreach (var w in ws)
            {
                rwSequences[w].Item2.AddRange(sg);
            }
        }
        return rwSequences;
    }


    internal void ReadForeach(Func<ValueWrapper, IEnumerable<KeyValuePair<object, object>>> transformAction)
    {

        var result = new List<ReadFocusEntry>();
        foreach (var r in Reads[Active.ReadId])
        {
            foreach (var kv in transformAction(r.Value))
            {
                _sequenceIdCounter++;
                result.Add(new ReadFocusEntry
                {
                    Key = new ValueWrapper(kv.Key),
                    Value = new ValueWrapper(kv.Value),
                    SequenceId = _sequenceIdCounter
                });
                Sequences[r.SequenceId].ChildrenIds.Add(_sequenceIdCounter);
                Sequences[_sequenceIdCounter] = new ParsingSequence { ParentId = r.SequenceId };
            }
        }

        _readIdCounter++;
        Reads[_readIdCounter] = result;
        Active = new ParsingFocus2(_readIdCounter, Active.WriteId);





#if DEBUG

        var w2 = Writes[Active.WriteId];
        var r2 = Reads[Active.ReadId];
        var ws = GenerateSequencesIntersection(w2, r2);
        var opt = new JsonSerializerOptions();
        opt.WriteIndented = false;
        var rr = $"[{r2.Select(x => x.SequenceId + "_" + JsonSerializer.Serialize(x.Value.V, opt)).Join(", ")}]";
        var ww = $"[{w2.Select(x => x.SequenceId + "_" + JsonSerializer.Serialize(x.Value.V, opt)).Join(", ")}]";

        var wf = $"{ws.Select(
            x => $"[{x.Value.Read.Select(y => JsonSerializer.Serialize(y.Value.V, opt)).Join(", ")}]"
        ).Join("\n")}";

        var s = 345345;
#endif
    }

    internal void WriteFlatten(Func<ValueWrapper, ValueWrapper> writeTransform)
    {
        var result = new List<WriteFocusEntry>();

        foreach (var x in GenerateSequencesIntersection(Writes[Active.WriteId], Reads[Active.ReadId]))
        {
            var w = x.Value.Write;
            var rs = x.Value.Read;
            result.AddRange(rs.Select(r => new WriteFocusEntry { SequenceId = r.SequenceId, Value = writeTransform(w.Value) }));
        }

        _writeIdCounter++;
        Writes[_writeIdCounter] = result;
        Active = new ParsingFocus2 { ReadId = Active.ReadId, WriteId = _writeIdCounter };
    }

}



internal struct ParsingFocus2
{

    internal int WriteId { get; set; }

    internal int ReadId { get; set; }

    internal ParsingFocus2(int readId, int writeId)
    {
        WriteId = writeId;
        ReadId = readId;
    }


}

internal struct ParsingSequence
{
    internal int ParentId { get; set; }
    internal List<int> ChildrenIds { get; set; } = [];

    public ParsingSequence()
    {

    }
}

internal class ReadFocusEntry
{
    internal ValueWrapper Key { get; set; }
    internal ValueWrapper Value { get; set; }
    internal int SequenceId { get; set; }
}

internal class WriteFocusEntry
{
    internal ValueWrapper Value { get; set; }
    internal int SequenceId { get; set; }
}
