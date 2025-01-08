using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Modules;

public class HierarchyModule : IWritingModule
{
    private Dictionary<string, (int Index, Type Type)> _types;

    public HierarchyModule(List<(string, Type)> types)
    {
        _types = new Dictionary<string, (int Index, Type Type)>();
        for (int i = 0; i < types.Count; i++) {

            var x = types[i];

            _types.Add(x.Item1, (i, x.Item2));
        }


    }

    public void Append(object target, object? val)
    {
        if (target is not HierarchyEntry m)
            throw new Exception($"{nameof(HierarchyModule)} - Can't append to {target.GetType().Name}");

        if (val is not HierarchyEntry v)
            throw new Exception($"{nameof(HierarchyModule)} - Can't append {target.GetType().Name}");

        if (m.Entries == null)
            m.Entries = new List<HierarchyEntry>();

        m.Entries.Add(v);
    }

    public object BlankArray() => new HierarchyEntry();

    public object BlankMap() => new HierarchyEntry();


    public void Write(object target, string acc, object? val)
    {
        if (target is not HierarchyEntry m)
            throw new Exception($"{nameof(HierarchyModule)} - Can't read {target.GetType().Name}");

        if (!_types.TryGetValue(acc, out var x))
            throw new Exception($"{nameof(HierarchyModule)} - Unknown accessor '{acc}' | valid = [{_types.Keys.Select(x => $"'{x}'").Join(", ")}]");

        if (m.Values == null)
            m.Values = new object[_types.Count];

        m.Values[x.Index] = val;
    }

    public void Write(object target, int acc, object? val)
    {
        throw new NotImplementedException();
    }
}


public class HierarchyEntry()
{
    public object[] Values { get; set; }
    public List<HierarchyEntry> Entries { get; set; }

    public List<object[]> ExtractEntries()
    {
        var stack = new List<(HierarchyEntry Entry, object[]? V)> { (this, Values != null ? new object[Values.Length] : null) };
        var result = new List<object[]>();

        while (stack.TryPop(out var p))
        {

            object[] v2 = null; 
            if (p.Entry.Values != null || p.V != null)
            {
                if (p.V == null)
                {
                    v2 = p.Entry.Values.ToList().ToArray();
                }
                else if (p.Entry.Values == null)
                {
                    v2 = p.V.ToList().ToArray();
                }
                else {
                    v2 = new object[p.V.Length];
                    for (var i = 0; i < p.V.Length; i++)
                    {
                        var v = p.Entry.Values[i];
                        if (v != null)
                            v2[i] = v;
                        else
                            v2[i] = p.V[i];
                    }
                }
            }

            if (p.Entry.Entries != null && p.Entry.Entries.Count > 0)
            {
                foreach (var e in p.Entry.Entries)
                    stack.Add((e, v2));
            }
            else if(v2 != null)
            {
                result.Add(v2);
            }
        }
        return result;
    }
}