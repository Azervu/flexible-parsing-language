using System.Text.RegularExpressions;

namespace FlexibleParsingLanguage.Functions;

internal class RegexFilter : IFilterFunction_String
{
    //TODO make pre compiled filter
    public bool Filter(object value, string acc)
    {
        if (value is not string s)
            s = value?.ToString() ?? string.Empty;

        var reg = new Regex(acc);
        return reg.IsMatch(s);
    }
}
