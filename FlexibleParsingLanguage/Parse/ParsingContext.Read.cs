using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Parse;

internal partial class ParsingContext
{
    internal void ReadAction(Func<IReadingModule, object, object> readTransform) => MapFocus((x) => ReadInner(x, readTransform));
    internal void ReadTransform(Func<ParsingFocusEntry, object, object> readTransform) => MapFocus((x) => ReadTransformInner(x, readTransform));
    internal void ReadName() => MapFocus(ReadNameInner);
    internal void ReadFlatten() => MapFocus(ReadFlattenInner);

    internal void ReadConfig() => MapFocus(ReadFlattenInner);

    private ParsingFocusEntry ReadInner(ParsingFocusEntry focus, Func<IReadingModule, object, object> readTransform)
    {
        var newReads = new List<object>();
        foreach (var r in focus.Reads)
        {
            UpdateReadModule(r);
            newReads.Add(readTransform(ReadingModule, r));
        }
        return new ParsingFocusEntry
        {
            Reads = newReads,
            Write = focus.Write,
            Keys = focus.Reads,
            MultiRead = focus.MultiRead,
            Config = focus.Config,
        };
    }

    private ParsingFocusEntry ReadTransformInner(ParsingFocusEntry focus, Func<ParsingFocusEntry, object, object> readTransform) => new ParsingFocusEntry
    {
        Reads = focus.Reads.Select(x => readTransform(focus, x)).ToList(),
        Write = focus.Write,
        Keys = focus.Reads,
        MultiRead = focus.MultiRead,
        Config = focus.Config,
    };

    private ParsingFocusEntry ReadNameInner(ParsingFocusEntry focus) => new ParsingFocusEntry
    {
        Reads = focus.Keys,
        Write = focus.Write,
        Keys = focus.Keys,
        MultiRead = focus.MultiRead,
        Config = focus.Config,
    };

    private ParsingFocusEntry ReadFlattenInner(ParsingFocusEntry focus)
    {
        var keys = new List<object>();
        var innerResult = new List<object>();
        foreach (var read in focus.Reads)
        {
            UpdateReadModule(read);
            foreach (var (k, v) in ReadingModule.Foreach(read))
            {
                keys.Add(k);
                innerResult.Add(v);
            }

        }
        return new ParsingFocusEntry
        {
            Keys = keys,
            Reads = innerResult,
            MultiRead = true,
            Write = focus.Write,
            Config = focus.Config,
        };
    }


    private ParsingFocusEntry ReadConfigInner(ParsingFocusEntry focus)
    {
        var keys = new List<object>();
        var innerResult = new List<object>();
        foreach (var read in focus.Reads)
        {
            UpdateReadModule(read);
            foreach (var (k, v) in ReadingModule.Foreach(read))
            {
                keys.Add(k);
                innerResult.Add(v);
            }

        }
        return new ParsingFocusEntry
        {
            Keys = keys,
            Reads = innerResult,
            MultiRead = true,
            Write = focus.Write,
            Config = focus.Config,
        };
    }


    private void UpdateReadModule(object obj)
    {
        var t = obj?.GetType() ?? typeof(void);
        if (t != _activeType)
        {
            _activeType = t;
            ReadingModule = _modules.LookupModule(t);
        }
    }

    private object TransformRead(object raw)
    {
        UpdateReadModule(raw);
        if (ReadingModule == null)
            return raw;
        return ReadingModule.ExtractValue(raw);
    }
}