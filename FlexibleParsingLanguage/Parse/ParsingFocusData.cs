using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

#if DEBUG
        if (!Store.ContainsKey(id))
        {
            var s = 345345;
        }
#endif

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

        var sequenceIntersections = GenerateSequencesIntersection2(
            writes.Select(x => x.SequenceId).ToList(),
            reads.Select(x => x.Select(y => y.SequenceId).ToList()).ToArray()
        );


        if (sequenceIntersections.Count != writes.Count)
            throw new Exception(">>>>>>>>>>>>>> A");

        var result = new List<SequenceIntersection>(sequenceIntersections.Count);
        for (var i = 0; i < sequenceIntersections.Count; i++)
        {
            var w = writes[i];
            var sequence = sequenceIntersections[i];

            if (reads.Count() != sequence.Intersected.Count())
                throw new Exception(">>>>>>>>>>>>>> B");

            var intersected = new SequenceIntersection.SequenceIntersectionEntry[sequence.Intersected.Count()];
            for (var j = 0; j < sequence.Intersected.Count(); j++)
            {
                var r = reads[j];
                var ii = sequence.Intersected[j];


                //if (r.Count() != ii.Foci.Count)
                 //   throw new Exception(">>>>>>>>>>>>>> C");

                intersected[j] = new SequenceIntersection.SequenceIntersectionEntry
                {
                    Multiread = ii.Multiread,
                    Foci = ii.Foci.Select(x => r[x.Index]).ToList(),
                };
            }

            result.Add(new SequenceIntersection
            {
                SequenceId = sequence.SequenceId,
                Primary = writes[i],
                Intersected = intersected
            });
        }

        return result;



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



#if DEBUG
            if (r.GroupBy(x => x.SequenceId).Any(x => x.Count() > 1))
                throw new Exception("Multiple sequences");
#endif



            foreach (var sg in r)
            {
                var activeId = sg.SequenceId;
                List<int>? ws = null;
                while (!writeChildSequences.TryGetValue(activeId, out ws))
                {
                    activeId = Sequences[activeId].ParentId;
                    if (activeId < 0)
                        throw new InvalidOperationException($"read/write missing shared root | writes = [{writeChildSequences.Select(x => x.Key.ToString()).Join(", ")}] | reads = [{r.GroupBy(x => x.SequenceId).Select(x => x.Key.ToString()).Join(", ")}]");
                }
                foreach (var w in ws)
                {
                    rwSequences[w].Item2[i].Add(sg);
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
                SequenceId = x.Key,
                Primary = x.Value.Write,
                Intersected = inter.ToArray(),
            };
        }).ToList();
    }





    internal List<SequenceIntersection2> GenerateSequencesIntersection2(List<int> primeSequence, List<int>[] secondarySequence)
    {
        var rwSequences = new Dictionary<int, (int Prime, List<(int SequenceId, int Index)>[] Secondary)>();

        foreach (var prime in primeSequence)
        {
            if (rwSequences.ContainsKey(prime))
                throw new InvalidOperationException("multiple write heads on same sequence");
            rwSequences[prime] = (prime, secondarySequence.Select(x => new List<(int SequenceId, int Index)>()).ToArray());
        }

        Dictionary<int, List<int>> writeChildSequences = new();
        foreach (var prime in primeSequence)
        {
            var activeId = prime;
            while (activeId >= 0)
            {
                if (!writeChildSequences.TryGetValue(activeId, out var values))
                {
                    values = [prime];
                    writeChildSequences.Add(activeId, values);
                }
                else
                {
                    values.Add(prime);
                }
                activeId = Sequences[activeId].ParentId;
            }
        }

        for (var i = 0; i < secondarySequence.Length; i++)
        {
            var r = secondarySequence[i];

            for (var j = 0; j < r.Count; j++)
            {
                var sequentialId = r[j];
                var activeId = sequentialId;
                List<int>? ws = null;
                while (!writeChildSequences.TryGetValue(activeId, out ws))
                {
                    activeId = Sequences[activeId].ParentId;
                    if (activeId < 0)
                        throw new InvalidOperationException($"read/write missing shared root | writes = [{writeChildSequences.Select(x => x.Key.ToString()).Join(", ")}] | reads = [{r.Select(x => x.ToString()).Join(", ")}]");
                }
                foreach (var w in ws)
                {
                    rwSequences[w].Secondary[i].Add((sequentialId, j));
                }
            }

            /*
            foreach (var sequentialId in secondarySequence[i])
            {
                var activeId = sequentialId;
                List<int>? ws = null;
                while (!writeChildSequences.TryGetValue(activeId, out ws))
                {
                    activeId = Sequences[activeId].ParentId;
                    if (activeId < 0)
                        throw new InvalidOperationException($"read/write missing shared root | writes = [{writeChildSequences.Select(x => x.Key.ToString()).Join(", ")}] | reads = [{r.Select(x => x.ToString()).Join(", ")}]");
                }
                foreach (var w in ws)
                {
                    rwSequences[w].Secondary[i].Add((sequentialId, i));
                }
            }
            */
        }

        /*
         
                    var secondary = new List<(int SequenceId, int Index)>[secondarySequence.Length];
            for (var i = 0; i < secondarySequence.Length; i++)
            {
                secondary[i] = 
            }


         */

        return primeSequence.Select(sequenceId =>
        {
            var sequence = rwSequences[sequenceId];
            var multiRead = true;
            var primaryAncestors = new HashSet<int> { };
            while (sequenceId >= 0)
            {
                primaryAncestors.Add(sequenceId);
                sequenceId = Sequences[sequenceId].ParentId;
            }

            var inter = sequence.Secondary.Select(x =>
            {
                var multiRead = true;
                if (x.Count > 0)
                {
                    var sequences = x.ToHashSet();
                    if (sequences.Count == 1)
                        multiRead = !primaryAncestors.Contains(sequences.First().SequenceId);
#if DEBUG
                    if (!multiRead && x.Count > 1)
                        throw new Exception("multiple in same sequence");
#endif
                }
                return new SequenceIntersectionEntry
                {
                    Multiread = multiRead,
                    Foci = x,
                };
            });

            return new SequenceIntersection2
            {

                SequenceId = sequenceId,
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







internal struct SequenceIntersection2
{
    internal int SequenceId { get; set; }
    internal int PrimeIndex { get; set; }
    internal SequenceIntersectionEntry[] Intersected { get; set; }
}

internal struct SequenceIntersectionEntry
{
    internal bool Multiread { get; set; }
    internal List<(int SequenceId, int Index)> Foci { get; set; }
}








internal struct SequenceIntersection
{
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