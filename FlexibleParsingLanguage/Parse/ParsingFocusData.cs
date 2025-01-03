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

    internal Dictionary<int, List<FocusEntry>> Reads { get; set; }

    internal Dictionary<int, List<FocusEntry>> Writes { get; set; }

    internal ParsingFocus2 Active { get; set; }

    internal ParsingFocusData(ParsingMetaContext parsingConfig, object readRoot, object writeRoot)
    {
        Sequences = new Dictionary<int, ParsingSequence> {
            { _sequenceIdCounter, new ParsingSequence { ParentId = -1 } }
        };

        Reads = new Dictionary<int, List<FocusEntry>>
        {
            { _readIdCounter, [ new FocusEntry { Value = new ValueWrapper(readRoot), SequenceId = _sequenceIdCounter } ] }
        };

        Writes = new Dictionary<int, List<FocusEntry>>
        {
            { _writeIdCounter, [ new FocusEntry { Value = new ValueWrapper(writeRoot), SequenceId = _sequenceIdCounter } ] }
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
        return new FocusEntry { Key = kv.Key, Value = kv.Value, SequenceId = x.SequenceId, };
    });
    internal void ReadInner(Func<FocusEntry, FocusEntry> transform)
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

    internal void Write(Func<ValueWrapper, ValueWrapper> transform) => WriteInner((x) => new FocusEntry { Value = transform(x.Value), SequenceId = x.SequenceId });

    internal void WriteInner(Func<FocusEntry, FocusEntry> transform)
    {
        var writes = Writes[Active.WriteId];
        Writes[++_writeIdCounter] = writes.Select(transform).ToList();
        Active = new ParsingFocus2(Active.ReadId, _writeIdCounter);
    }

    internal void WriteFromRead(Func<FocusEntry, ValueWrapper> extractRead, Action<WriteParam> action)
    {
        var ws = GenerateSequencesIntersection(Writes[Active.WriteId], [Reads[Active.ReadId]]);
        foreach (var rw in ws)
        {
            var write = rw.Primary;
            var read = rw.Intersected[0];

            if (read.Foci.Count == 0)
                throw new Exception("no reads in sequence");
            var p = new WriteParam(read.Foci.Select(extractRead).ToList(), write.Value, read.Multiread);
            action(p);
        }
    }


    internal List<SequenceIntersection> GenerateSequencesIntersection(List<FocusEntry> writes, List<FocusEntry>[] reads)
    {
        var rwSequences = new Dictionary<int, (FocusEntry Write, List<FocusEntry>[] Read)>();

        foreach (var w in writes)
        {
            if (rwSequences.ContainsKey(w.SequenceId))
                throw new InvalidOperationException("multiple write heads on same sequence");
            rwSequences[w.SequenceId] = (w, reads.Select(x => new List<FocusEntry>()).ToArray());
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

        for (var i = 0; i < reads.Length; i++)
        {
            var r = reads[i];

            foreach (var sg in r.GroupBy(x => x.SequenceId))
            {
                var activeId = sg.Key;
                List<int>? ws = null;
                while (!writeChildSequences.TryGetValue(activeId, out ws))
                {
                    activeId = Sequences[activeId].ParentId;
                    if (activeId < 0)
                        throw new InvalidOperationException($"read/write missing shared root | writes = [{writeChildSequences.Select(x => x.Key.ToString()).Join(", ")}] | reads = [{r.GroupBy(x => x.SequenceId).Select(x => x.Key.ToString()).Join(", ")}]");
                }
                foreach (var w in ws)
                {
                    rwSequences[w].Item2[i].AddRange(sg);
                }
            }
        }

        return rwSequences.Select(x =>
        {
            var multiRead = true;
            var sequenceId = x.Key;
            var primaryAncestors = new HashSet<int> {};
            while (sequenceId >= 0)
            {
                primaryAncestors.Add(sequenceId);
                sequenceId = Sequences[sequenceId].ParentId;
            }

            var inter = x.Value.Read.Select(x =>
            {
                var multiRead = true;
                if (x.Count > 0)
                {
                    var sequences = x.Select(y => y.SequenceId).ToHashSet();
                    if (sequences.Count == 1)
                        multiRead = !primaryAncestors.Contains(sequences.First());
#if DEBUG
                    if (!multiRead && x.Count > 1)
                        throw new Exception("multiple in same sequence");
#endif
                }
                return new SequenceIntersection.SequenceIntersectionEntry
                {
                    Multiread = multiRead,
                    Foci = x,
                };
            });

            return new SequenceIntersection
            {
                MultiRead = multiRead,
                SequenceId = x.Key,
                Primary = x.Value.Write,
                Intersected = inter.ToArray(),
            };
        }).ToList();
    }

    internal void ReadForeach(Func<ValueWrapper, IEnumerable<KeyValuePair<object, object>>> transformAction)
    {

        var result = new List<FocusEntry>();
        foreach (var r in Reads[Active.ReadId])
        {
            foreach (var kv in transformAction(r.Value))
            {
                _sequenceIdCounter++;
                result.Add(new FocusEntry
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
    }

    internal void WriteFlatten(Func<ValueWrapper, ValueWrapper> writeTransform)
    {
        var result = new List<FocusEntry>();

        foreach (var x in GenerateSequencesIntersection(Writes[Active.WriteId], [Reads[Active.ReadId]]))
        {
            var w = x.Primary;
            var rs = x.Intersected[0];
            result.AddRange(rs.Foci.Select(r => new FocusEntry { SequenceId = r.SequenceId, Value = writeTransform(w.Value) }));
        }

        _writeIdCounter++;
        Writes[_writeIdCounter] = result;
        Active = new ParsingFocus2 { ReadId = Active.ReadId, WriteId = _writeIdCounter };
    }

}








internal struct SequenceIntersection
{
    internal bool MultiRead { get; set; }
    internal int SequenceId { get; set; }
    internal FocusEntry Primary { get; set; }
    internal SequenceIntersectionEntry[] Intersected { get; set; }
    internal struct SequenceIntersectionEntry
    {
        internal bool Multiread { get; set; }
        internal List<FocusEntry> Foci { get; set; }
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

internal class FocusEntry
{
    internal ValueWrapper Key { get; set; }
    internal ValueWrapper Value { get; set; }
    internal int SequenceId { get; set; }
}