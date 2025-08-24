using FlexibleParsingLanguage.Compiler;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FlexibleParsingLanguage.Parse;

internal class ParsingFocusData
{
    internal int SequenceIdCounter = 1;

    internal Dictionary<int, ParsingNode> Store { get; set; }

    internal Dictionary<int, ParsingSequence> Sequences { get; private set; } // childId to parent id

    internal Dictionary<int, List<FocusEntry>> Reads { get; set; }
    internal Dictionary<int, List<FocusEntry>> Writes { get; set; }
    internal Dictionary<int, List<ConfigEntry>> Configs { get; set; }

    internal ParsingNode Active { get; set; }

    internal ParsingFocusData(ParsingMetaContext parsingConfig, object readRoot, object writeRoot)
    {
        Sequences = new Dictionary<int, ParsingSequence> {
            { SequenceIdCounter, new ParsingSequence { ParentId = -1 } }
        };

        Reads = new Dictionary<int, List<FocusEntry>>
        {
            { 1, [ new FocusEntry { Value = new ValueWrapper(readRoot), SequenceId = SequenceIdCounter } ] }
        };

        Writes = new Dictionary<int, List<FocusEntry>>
        {
            { 1, [ new FocusEntry { Value = new ValueWrapper(writeRoot), SequenceId = SequenceIdCounter } ] }
        };

        Configs = new Dictionary<int, List<ConfigEntry>>
        {
            { 1, [new ConfigEntry(parsingConfig, SequenceIdCounter) ] }
        };

        Active = new ParsingNode(1, 1, 1);
        Store = new Dictionary<int, ParsingNode> {
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
        Active = new ParsingNode(readId, Active.WriteId, Active.ConfigId);
    }

    internal void LoadWrite(int id)
    {
        var writeId = Store[id].WriteId;
        Active = new ParsingNode(Active.ReadId, writeId, Active.ConfigId);
    }

    internal void Read(int opId, Func<ValueWrapper, KeyValuePair<ValueWrapper, ValueWrapper>> transform) => ReadInner(opId, x =>
    {
        var kv = transform(x.Value);
        return new FocusEntry { Key = kv.Key, Value = kv.Value, SequenceId = x.SequenceId, };
    });

    internal void ReadInner(int opId, Func<FocusEntry, FocusEntry> transform)
    {
        NextRead(opId, Reads[Active.ReadId].Select(transform).ToList());
    }

    internal void NextRead(int opId, List<FocusEntry> reads)
    {
        Reads[opId] = reads;
        Active = new ParsingNode(opId, Active.WriteId, Active.ConfigId);
    }


    internal void ReadForeach(int opId, Func<FocusEntry, IEnumerable<KeyValuePair<object, object>>> transformAction)
    {
        var result = new List<FocusEntry>();
        foreach (var r in Reads[Active.ReadId])
        {
            foreach (var kv in transformAction(r))
            {
                SequenceIdCounter++;
                result.Add(new FocusEntry
                {
                    Key = new ValueWrapper(kv.Key),
                    Value = new ValueWrapper(kv.Value),
                    SequenceId = SequenceIdCounter
                });
                Sequences[r.SequenceId].ChildrenIds.Add(SequenceIdCounter);
                Sequences[SequenceIdCounter] = new ParsingSequence { ParentId = r.SequenceId };
            }
        }
        Reads[opId] = result;
        Active = new ParsingNode(opId, Active.WriteId, Active.ConfigId);
    }

    internal void Write(int opId, Func<ValueWrapper, ValueWrapper> transform) =>
        NextWrite(opId, Writes[Active.WriteId].Select((x) => new FocusEntry { Value = transform(x.Value), SequenceId = x.SequenceId }).ToList());
        
    internal void NextWrite(int opId, List<FocusEntry> next)
    {
        Writes[opId] = next;
        Active = new ParsingNode(Active.ReadId, opId, Active.ConfigId);
    }

    internal void NextConfig(int opId, List<ConfigEntry> next)
    {
        Configs[opId] = next;
        Active = new ParsingNode(Active.ReadId, Active.WriteId, opId);
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
#if DEBUG
        foreach (var parameter in secondarySequence)
        {
            foreach (var s in parameter)
            {
                var parentId = s;
                while (Sequences.TryGetValue(parentId, out var x))
                {
                    parentId = x.ParentId;
                }
                if (parentId > 0)
                    throw new InvalidOperationException($"Sequence {parentId} missing");
            }
        }
#endif

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
            while (Sequences.ContainsKey(activeId))
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
#if DEBUG
                    if (!Sequences.ContainsKey(activeId))
                        throw new InvalidOperationException($"Sequence missing | {activeId} | [{Sequences.Select(x => x.Key.ToString()).Join(", ")}]");
#endif

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


        var result = new List<(int SequenceId, SequenceIntersectionEntryInner[] Intersected)>(primeSequence.Count);

        for (var i = 0; i < primeSequence.Count; i++)
        {
            var sequenceId = primeSequence[i];
            var sequence = rwSequences[sequenceId];
            var primaryAncestors = new HashSet<int> { };
            var sequenceId2 = sequenceId;

            while (sequenceId2 >= 0)
            {
                primaryAncestors.Add(sequenceId2);
                sequenceId2 = Sequences[sequenceId2].ParentId;
            }

            var inter = new SequenceIntersectionEntryInner[sequence.Secondary.Length];

            for (var j = 0; j < sequence.Secondary.Length; j++)
            {
                var x = sequence.Secondary[j];
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
                inter[j] = new SequenceIntersectionEntryInner
                {
                    Multiread = multiRead,
                    Foci = x,
                };

            }
            result.Add((sequenceId, inter));
        }
        return result;
    }


#if DEBUG



    internal string DebugHierarchyString()
    {
        var sb = new StringBuilder();
        var active = new List<(int Id, int Depth)> { (1, 0) };
        while (active.Count > 0)
        {
            var x = active.Pop();
            var e = Sequences[x.Id];


            for (var i = 0; i < x.Depth; i++)
                sb.Append("|");
            sb.Append("+ ");
            sb.Append(x.Id);
            sb.Append("\n");
            foreach (var c in e.ChildrenIds)
                active.Add((c, x.Depth + 1));

        }
        return sb.ToString();
    }



    internal string DebugString()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Writes = [{string.Join(", ", Writes.Select(x => x.Key.ToString()))}]");
        sb.AppendLine($"Sequences = [{string.Join(", ", Sequences.Select(x => x.Key.ToString()))}]");

        foreach (var r in Reads)
            sb.AppendLine($"Read({r.Key}) = [{string.Join(", ", r.Value.Select(x => x.SequenceId.ToString()))}]");

        return sb.ToString();
    }

    internal void ValidateTree()
    {
        //return;

        var roots = new Dictionary<int, int>();
        var handledIds = new HashSet<int>();
        var rootChildren = new HashSet<int>();

        var directRootChildren = new HashSet<int>();
        foreach (var kv in Sequences)
        {
            if (kv.Value.ParentId < 0)
                directRootChildren.Add(kv.Key);
            else if (!Sequences.ContainsKey(kv.Value.ParentId))
                throw new Exception($"Sequence does not exist {kv.Value.ParentId}");
        }

        if (directRootChildren.Count > 1)
            throw new Exception($"Multiple roots [{directRootChildren.Select(x => x.ToString()).Join(", ")}]");


        if (roots.Count > 1)
            throw new Exception($"Multiple roots found in parsing tree: {roots.Count} | {roots.Select(x => x.ToString()).Join(", ")}");
        var debug = roots.Select(x => x.ToString()).Join("\n");

        foreach (var r in Reads)
        {
            foreach (var r2 in r.Value)
            {
                if (!Sequences.ContainsKey(r2.SequenceId))
                    throw new Exception($"Missing Sequence {r2.SequenceId} From Read");
            }
        }

        foreach (var w in Writes)
        {
            foreach (var w2 in w.Value)
            {
                if (!Sequences.ContainsKey(w2.SequenceId))
                    throw new Exception($"Missing Sequence {w2.SequenceId} From Write");
            }
        }
    }
#endif

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


internal struct ParsingNode
{
    internal int WriteId { get; private set; }
    internal int ReadId { get; private set; }
    internal int ConfigId { get; private set; }
    internal ParsingNode(int readId, int writeId, int configId)
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