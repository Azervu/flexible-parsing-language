using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace FlexibleParsingLanguage;

public static class Util
{
    public static string Join(this IEnumerable<string> it, string separator) => string.Join(separator, it);

    public static string Join(this IEnumerable<string> it, char separator) => string.Join(separator, it);

    public static string Concat(this IEnumerable<string> it) => string.Concat(it);

    public static T Pop<T>(this List<T> stack)
    {
        var v = stack[stack.Count - 1];
        stack.RemoveAt(stack.Count - 1);
        return v;
    }

    public static bool TryPop<T>(this List<T> stack, out T result)
    {
        if (stack.Count == 0)
        {
            result = default;
            return false;
        }

        result = stack[stack.Count - 1];
        stack.RemoveAt(stack.Count - 1);
        return true;
    }

    public static IEnumerable<T> LazyConcat<T>(this IEnumerable<T> it, params IEnumerable<T>[] iterators) {
        foreach (var item in it) {
            yield return item;        
        }

        foreach (var it2 in iterators)
        {
            foreach (var item in it2)
            {
                yield return item;
            }
        }
    }

    public static IEnumerable<T> LazyConcat<T>(params IEnumerable<T>[] iterators)
    {
        foreach (var it in iterators)
        {
            foreach (var item in it)
            {
                yield return item;
            }
        }
    }

    public static IEnumerable<T[]> LazyZip<T>(params IEnumerable<T>[] iterators)
    {
        var its = iterators.Select(x => x.GetEnumerator()).ToList();
        var response = new T[its.Count];
        var handled = true;
        while (handled)
        {
            handled = false;
            for (var i = 0; i < its.Count; i++)
            {
                var it = its[i];
                if (it.MoveNext())
                {
                    response[i] = it.Current;
                    handled = true;
                }
                else
                {
                    response[i] = default;
                }
            }

            if (handled)
                yield return response;
        }
    }


    public static string GetHumanReadableName(this Type t)
    {
        var name = new StringBuilder();
        GetHumanReadableNameInner(name, t);
        return name.ToString();
    }


    private static void GetHumanReadableNameInner(StringBuilder name, Type t)
    {
        if (t.IsGenericType)
            HandleGenericType(name, t);
        else
            HandleNonGenericType(name, t);
    }


    private static void HandleNonGenericType(StringBuilder name, Type t)
    {

        if (t == typeof(float))
        {
            name.Append("float");
            return;
        }

        if (t == typeof(int))
        {
            name.Append("int");
            return;
        }

        if (t == typeof(string))
        {
            name.Append("string");
            return;
        }



        if (t.BaseType == typeof(Array))
        {
            GetHumanReadableNameInner(name, t.GetMethods()[0].ReturnType);
            name.Append("[]");
            return;
        }
        name.Append(t.Name);
    }




    private static void HandleGenericType(StringBuilder name, Type t)
    {
        var g = t.GetGenericTypeDefinition();

        if (g == typeof(Nullable<>))
        {
            GetHumanReadableNameInner(name, t.GenericTypeArguments[0]);
            name.Append('?');
            return;
        }

        if (t.Name.StartsWith("ValueTuple"))
        {
            name.Append('(');
            GetFieldNames(name, t);
            name.Append(')');
            return;
        }

        name.Append(g.Name.Split('`')[0]);
        name.Append('<');
        GetGenericArgumentsNames(name, t);
        name.Append('>');
    }





    private static void GetGenericArgumentsNames(StringBuilder name, Type t)
    {
        var a = t.GetGenericArguments();
        for (var i = 0; i < a.Length; i++)
        {
            GetHumanReadableNameInner(name, a[i]);
            if (i < a.Length - 1)
                name.Append(", ");
        }
    }

    private static void GetFieldNames(StringBuilder name, Type t)
    {
        var fields = t.GetFields();
        for (var i = 0; i < fields.Length; i++)
        {
            GetHumanReadableNameInner(name, fields[i].FieldType);
            if (i < fields.Length - 1)
                name.Append(", ");
        }
    }
}


public sealed class CharEnumerator : IEnumerator, IEnumerator<char>, IDisposable, ICloneable
{
    private string _str; // null after disposal
    public int Index { get; private set; } = -1;

    internal CharEnumerator(string str) => _str = str;

    public object Clone() => MemberwiseClone();

    public bool MoveNext()
    {
        int index = Index + 1;
        int length = _str.Length;

        if (index < length)
        {
            Index = index;
            return true;
        }

        Index = length;
        return false;
    }

    public void Dispose() => _str = null!;

    object? IEnumerator.Current => Current;

    public char Current
    {
        get
        {
            if (!Valid)
                throw new InvalidOperationException("Enumeration already finished");
            return _str[Index];
        }
    }

    public void Reset() => Index = -1;

    public bool Valid { get => Index < _str.Length; }
}


public class MultiMap<K, V>
{
    private Dictionary<K, HashSet<V>> _dict = new();
    private Dictionary<V, HashSet<K>> _inverse = new();

    public int Count => _dict.Count;

    public void Add(K key, V value)
    {
        Add(_dict, key, value);
        Add(_inverse, value, key);
    }

    private static void Add<K, V>(Dictionary<K, HashSet<V>> dict, K k, V v)
    {
        if (dict.TryGetValue(k, out var set))
        {
            set.Add(v);
            return;
        }
        set = new HashSet<V> { v };
        dict.Add(k, set);
    }

    public void Remove(K key)
    {
        if (!_dict.TryGetValue(key, out var values))
            return;

        _dict.Remove(key);
        foreach (var v in values)
            _inverse.Remove(v);
    }

    public void RemoveInv(V key)
    {
        if (!_inverse.TryGetValue(key, out var values))
            return;
        _inverse.Remove(key);
        foreach (var v in values)
            _dict.Remove(v);
    }

    public bool TryGet(K key, out HashSet<V> value) => _dict.TryGetValue(key, out value);

    public bool TryGetInv(V key, out HashSet<K> value) => _inverse.TryGetValue(key, out value);

}