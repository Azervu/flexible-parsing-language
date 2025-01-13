using System.Text.RegularExpressions;

namespace FlexibleParsingLanguage.Functions;

internal class RegexFilter : IFilterFunction
{
    public string Name => "regex";

    public Type[] ParameterTypes => [typeof(string)];

    public bool Filter(object value, object[] p)
    {
        var acc = (string)p[0];
        if (value is not string s)
            s = value?.ToString() ?? string.Empty;

        var reg = new Regex(acc);
        return reg.IsMatch(s);
    }
}
