using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            { _sequenceIdCounter, new ParsingSequence { ParentId = -1, Length = 1 } }
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

            if (sameSequence && r.Count != 1)
                throw new Exception($"to many reads in same sequence | [{r.Select(x => x.SequenceId.ToString()).Join(", ")}]");

            var readValues = r.Select(extractRead).ToList();

            action(new WriteParam(readValues, w.Value, !sameSequence));
        }
    }

    private new Dictionary<int, (WriteFocusEntry Write, List<ReadFocusEntry> Read)> GenerateSequencesIntersection(List<WriteFocusEntry> writes, List<ReadFocusEntry> reads)
    {
        var rwSequences = new Dictionary<int, (WriteFocusEntry, List<ReadFocusEntry>)>();

        foreach (var w in writes)
        {
            if (rwSequences.ContainsKey(w.SequenceId))
                throw new InvalidOperationException("multiple write heads on same sequence");
            rwSequences[w.SequenceId] = (w, new());
        }

        /*
        var writeSequences = writes
            .GroupBy(x => x.SequenceId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var readSequences = reads
            .GroupBy(x => x.SequenceId)
            .ToDictionary(x => x.Key, x => x.ToList());
                */

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

    /*
    
        internal void WriteFromRead(Func<ParsingFocusRead, object> readFunc, Action<IWritingModule, ParsingFocusEntry, object> writeAction)
    {
        foreach (var focusEntry in Focus.Entries)
        {
            var r = focusEntry.MultiRead
                ? focusEntry.Reads.Select(readFunc).ToList()
                : readFunc(focusEntry.Reads[0]);

            writeAction(WritingModule, focusEntry, r);
        }
    }


     */





    internal void ReadForeach(Func<ValueWrapper, IEnumerable<KeyValuePair<object, object>>> transformAction)
    {

        var result = new List<ReadFocusEntry>();
        foreach (var r in Reads[Active.ReadId])
        {
            var n = 0;
            foreach (var kv in transformAction(r.Value))
            {
                result.Add(new ReadFocusEntry
                {
                    Key = new ValueWrapper(kv.Key),
                    Value = new ValueWrapper(kv.Value),
                    SequenceId = _sequenceIdCounter
                });
                n++;
            }

            Sequences[_sequenceIdCounter].ChildrenIds.Add(_sequenceIdCounter + 1);
            _sequenceIdCounter++;
            Sequences[_sequenceIdCounter] = new ParsingSequence { ParentId = r.SequenceId, Length = n };





            /*

            var outReads = transformAction(r.Value).Select(x => new ReadFocusEntry
            {
                Key = new ValueWrapper(x.Key),
                Value = new ValueWrapper(x.Value),
                SequenceId = _sequenceIdCounter
            }).ToList();
            _readIdCounter++;


#if DEBUG
            if (outReads.Count == 0)
                throw new Exception("ReadForeach resulted in no reads");
#endif



            Reads[_readIdCounter] = outReads;
            Sequences[r.SequenceId].ChildrenIds.Add(_readIdCounter);

           */
        }



        _readIdCounter++;
        Reads[_readIdCounter] = result;
        Active = new ParsingFocus2(_readIdCounter, Active.WriteId);
    }



    internal void WriteFlatten(Func<ValueWrapper, ValueWrapper> writeTransform)
    {

        /*
        var nextWrites = new List<WriteFocusEntry>();
        foreach (var w in Writes[Active.WriteId])
        {
            var children = Sequences[w.SequenceId].ChildrenIds;


            nextWrites.AddRange(children.Select());

            var s = 45646;
        }
        */




        var rawWrites = Writes[Active.WriteId];

        foreach (var rw in rawWrites)
        {
            var children = Sequences[rw.SequenceId].ChildrenIds;
            var s = 345653;
        }

        var writes = rawWrites
            .SelectMany(w => Sequences[w.SequenceId].ChildrenIds.Select(cId => new WriteFocusEntry { SequenceId = cId, Value = writeTransform(w.Value) }))
            .ToList();

        _writeIdCounter++;
        Writes[_writeIdCounter] = writes;

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
    internal int Length { get; set; }
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
