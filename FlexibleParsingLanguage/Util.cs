using System.Text;

namespace FlexibleParsingLanguage;

public static class Util
{
    public static string Join(this IEnumerable<string> it, string separator) => string.Join(separator, it);

    public static string Join(this IEnumerable<string> it, char separator) => string.Join(separator, it);


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
        foreach (var it2 in iterators)
        {
            foreach (var item in it2)
            {
                yield return item;
            }
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
