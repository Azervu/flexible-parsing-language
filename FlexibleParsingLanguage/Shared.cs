using FlexibleParsingLanguage.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage;

public struct WriteParam
{
    public bool MultiRead { get; private set; }
    public List<ValueWrapper> Read { get; private set; }
    public ValueWrapper Write { get; private set; }

    public WriteParam(List<ValueWrapper> read, ValueWrapper write, bool multiRead)
    {
        Read = read;
        Write = write;
        MultiRead = multiRead;
    }
}

public struct ValueWrapper
{
    public object V { get; private set; }

    internal ValueWrapper(object v)
    {
#if DEBUG
        if (v is ValueWrapper vw)
            throw new ArgumentException($"double wrapped value | inner type {vw.V?.GetType().Name ?? "null"}");
#endif


        V = v;
    }
}