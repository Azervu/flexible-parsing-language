using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage;

public interface IReadingModule
{
    public List<Type> HandledTypes { get; }
    public object Parse(object raw, string acc);
    public object Parse(object raw, int acc);
    public IEnumerable<KeyValuePair<object, object>> Foreach(object raw);
    public object ExtractValue(object? val);
}




public interface IWritingModule
{
    public object BlankMap();
    public object BlankArray();
    public void Write(object target, string acc, object? val);
    public void Write(object target, int acc, object? val);
    public void Append(object target, object? val);
}

public interface IConverterFunction
{
    public object Convert(object value);
}

public interface IFilterFunction
{
    public bool Filter(object value);
}