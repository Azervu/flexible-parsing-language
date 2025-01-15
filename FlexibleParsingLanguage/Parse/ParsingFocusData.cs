using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FlexibleParsingLanguage.Parse;

internal class ParsingFocusData
{
    private int _sequenceIdCounter = 1;
    private int _readIdCounter = 1;
    private int _writeIdCounter = 1;
    private int _configIdCounter = 1;

    internal Dictionary<int, ParsingFocus> Store { get; set; }

    internal Dictionary<int, ParsingSequence> Sequences { get; set; } // childId to parent id

    internal Dictionary<int, List<FocusEntry>> Reads { get; set; }
    internal Dictionary<int, List<FocusEntry>> Writes { get; set; }
    internal Dictionary<int, List<ConfigEntry>> Configs { get; set; }

    internal ParsingFocus Active { get; set; }

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

        Configs = new Dictionary<int, List<ConfigEntry>>
        {
            { _configIdCounter, [new ConfigEntry(parsingConfig, _sequenceIdCounter) ] }
        };

        Active = new ParsingFocus(_writeIdCounter, _readIdCounter, _configIdCounter);

        Store = new Dictionary<int, ParsingFocus> {
            { Compiler.FplCompiler.RootId, Active }
        };
    }

    internal void Save(int id)
    {
        Store[id] = Active;
        Store[id] = Active;
    }

    internal void Load(int id)
    {
        Active = Store[id];
    }

    internal void LoadRead(int id)
    {
        var readId = Store[id].ReadId;
        Active = new ParsingFocus(readId, Active.WriteId, Active.ConfigId);
    }

    internal void LoadWrite(int id)
    {
        var writeId = Store[id].WriteId;
        Active = new ParsingFocus(Active.ReadId, writeId, Active.ConfigId);
    }

    internal void Read(Func<ValueWrapper, KeyValuePair<ValueWrapper, ValueWrapper>> transform) => ReadInner(x =>
    {
        var kv = transform(x.Value);
        return new FocusEntry { Key = kv.Key, Value = kv.Value, SequenceId = x.SequenceId, };
    });


    internal void ReadInner(Func<FocusEntry, FocusEntry> transform)
    {
        NextRead(Reads[Active.ReadId].Select(transform).ToList());
    }

    internal void NextRead(List<FocusEntry> reads)
    {
        Reads[++_readIdCounter] = reads;
        Active = new ParsingFocus(_readIdCounter, Active.WriteId, Active.ConfigId);
    }


    internal void ReadForeach(Func<FocusEntry, IEnumerable<KeyValuePair<object, object>>> transformAction)
    {
        var result = new List<FocusEntry>();
        foreach (var r in Reads[Active.ReadId])
        {
            foreach (var kv in transformAction(r))
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
        Active = new ParsingFocus(_readIdCounter, Active.WriteId, Active.ConfigId);
    }

    internal void Write(Func<ValueWrapper, ValueWrapper> transform) =>
        NextWrite(Writes[Active.WriteId].Select((x) => new FocusEntry { Value = transform(x.Value), SequenceId = x.SequenceId }).ToList());
        
    internal void NextWrite(List<FocusEntry> next)
    {
        Writes[++_writeIdCounter] = next;
        Active = new ParsingFocus(Active.ReadId, _writeIdCounter, Active.ConfigId);
    }

    internal void NextConfig(List<ConfigEntry> next)
    {
        Configs[++_configIdCounter] = next;
        Active = new ParsingFocus(Active.ReadId, Active.WriteId, _configIdCounter);
    }

    internal List<SequenceIntersection<T, A>> GenerateSequencesIntersection<T, A>(
        List<T> primeValues, List<int> primeSequence,
        List<A> aValues, List<int> aSequence
        )
    {
        var sequenceIntersections = GenerateSequencesIntersectionInner(primeSequence, [aSequence]);

#if DEBUG
        if (sequenceIntersections.Count != primeValues.Count)
            throw new Exception("nume prime vs intersection mismatch");
#endif

        var result = new List<SequenceIntersection<T, A>>(sequenceIntersections.Count);
        for (var i = 0; i < sequenceIntersections.Count; i++)
        {
            var sequence = sequenceIntersections[i];
            result.Add(new SequenceIntersection<T, A>
            {
                Primary = primeValues[i],
                AVal = new SequenceIntersectionEntry<A>(sequence.Intersected[0], aValues)
            });
        }
        return result;
    }

    internal struct SequenceIntersectionEntryInner
    {
        internal bool Multiread { get; set; }
        internal List<(int SequenceId, int Index)> Foci { get; set; }
    }

    internal List<(int SequenceId, SequenceIntersectionEntryInner[] Intersected)> GenerateSequencesIntersectionInner(List<int> primeSequence, List<int>[] secondarySequence)
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
                    if (!Sequences.ContainsKey(activeId))
                    {
                        var s = 345435;
                    }

                    activeId = Sequences[activeId].ParentId;
                    if (activeId < 0)
                        throw new InvalidOperationException($"read/write missing shared root | writes = [{writeChildSequences.Select(x => x.Key.ToString()).Join(", ")}] | reads = [{r.Select(x => x.ToString()).Join(", ")}]");
                }
                foreach (var w in ws)
                {
                    rwSequences[w].Secondary[i].Add((sequentialId, j));
                }
            }
        }

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
                return new SequenceIntersectionEntryInner
                {
                    Multiread = multiRead,
                    Foci = x,
                };
            });

            return (sequenceId, inter.ToArray());
        }).ToList();
    }
}

internal struct SequenceIntersection<T, A>
{
    internal T Primary { get; set; }
    internal SequenceIntersectionEntry<A> AVal { get; set; }
}


internal struct SequenceIntersectionEntry<T>
{
    internal bool Multiread { get; set; }
    internal List<T> Foci { get; set; }

    internal SequenceIntersectionEntry(ParsingFocusData.SequenceIntersectionEntryInner ii, List<T> raw)
    {
        Foci = ii.Foci.Select(x => raw[x.Index]).ToList();
        Multiread = ii.Multiread;
    }
}


internal struct ParsingFocus
{
    internal int WriteId { get; private set; }
    internal int ReadId { get; private set; }
    internal int ConfigId { get; private set; }
    internal ParsingFocus(int readId, int writeId, int configId)
    {
#if DEBUG
        if (readId == 0 && writeId == 0 && configId == 0)
            throw new Exception($"read = {readId} | write = {writeId} | config = {configId}");
#endif


        WriteId = writeId;
        ReadId = readId;
        ConfigId = configId;
    }
}

internal struct ParsingSequence
{
    internal int ParentId { get; set; }
    internal List<int> ChildrenIds { get; set; } = [];
    public ParsingSequence() {}
}

internal class FocusEntry
{
    internal ValueWrapper Key { get; set; }
    internal ValueWrapper Value { get; set; }
    internal int SequenceId { get; set; }
}

internal class ConfigEntry
{
    internal ParsingMetaContext Config { get; set; }
    internal int SequenceId { get; set; }

    internal ConfigEntry(ParsingMetaContext config, int sequenceId)
    {
        Config = config;
        SequenceId = sequenceId;
    }
}