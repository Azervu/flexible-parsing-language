using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Parse;

public struct WriteParam
{
    public object Read { get; internal set; }
    public object Write { get; internal set; }
}

internal class ParsingFocusData
{

    private int _sequenceIdCounter = 1;
    private int _readIdCounter = 1;
    private int _writeIdCounter = 1;




    internal Dictionary<int, ParsingFocus2> Storage { get; set; }

    internal Dictionary<int, ReadSequence> Sequences { get; set; } // childId to parent id

    internal Dictionary<int, List<ReadFocusEntry>> Reads { get; set; }

    internal Dictionary<int, List<WriteFocusEntry>> Writes { get; set; }

    internal ParsingFocus2 Active { get; set; }



    internal void Read(Func<object, object> transform)
    {
        var reads = Reads[Active.ReadId];
        Reads[++_readIdCounter] = reads.Select(x => new ReadFocusEntry { Data = transform(x.Data), SequenceId = x.SequenceId, }).ToList();
        Active = new ParsingFocus2(_readIdCounter, Active.WriteId);
    }

    internal void Write(Func<object, object> transform)
    {
        var writes = Writes[Active.WriteId];
        Writes[++_writeIdCounter] = writes.Select(x => new WriteFocusEntry { Data = transform(x.Data), SequenceId = x.SequenceId }).ToList();
        Active = new ParsingFocus2(Active.ReadId, _writeIdCounter);
    }


    internal void WriteFromRead(List<WriteFocusEntry> writes, List<ReadFocusEntry> reads, Func<object, object> extractRead, Action<WriteParam> action)
    {
        var ws = GenerateSequencesIntersection(writes, reads);
        foreach (var rw in ws)
        {
            var w = rw.Value.Item1;
            var r = rw.Value.Item2;

            if (r.Count == 0)
                throw new Exception("no reads in sequence");


            var sameSequence = w.SequenceId == r[0].SequenceId;


            if (sameSequence)
            {
                if (r.Count != 1)
                    throw new Exception($"to many reads in same sequence | [{r.Select(x => x.SequenceId.ToString()).Join(", ")}]");

                action(new WriteParam
                {
                    Read = extractRead(r[0].Data),
                    Write = w.Data
                });

                continue;
            }
            else
            {
                action(new WriteParam
                {
                    Read = r.Select(extractRead).ToList(),
                    Write = w.Data
                });

            }
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
                if (!writeChildSequences.TryGetValue(id, out var values))
                {
                    values = [id];
                    writeChildSequences.Add(id, values);
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
                    throw new InvalidOperationException("read/write missing shared root");
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


    internal void ReadForeach(Func<object, IEnumerable<object>> transformAction)
    {
        var reads = Reads[Active.ReadId];

        foreach (var r in reads)
        {
            _sequenceIdCounter++;
            var outReads = transformAction(r.Data).Select(x => new ReadFocusEntry
            {
                Data = x,
                SequenceId = _sequenceIdCounter
            }).ToList();
            _readIdCounter++;
            Reads[_readIdCounter] = outReads;
            Sequences[_sequenceIdCounter] = new ReadSequence { ParentId = r.SequenceId, Length = outReads.Count };
        }
        Active = new ParsingFocus2(_readIdCounter, Active.WriteId);
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



internal struct ReadSequence
{
    internal int ParentId { get; set; }
    internal int Length { get; set; }

}


internal class ReadFocusEntry
{
    internal object Data { get; set; }
    internal int SequenceId { get; set; }
}




internal class WriteFocusEntry
{
    internal object Data { get; set; }
    internal int SequenceId { get; set; }
}



