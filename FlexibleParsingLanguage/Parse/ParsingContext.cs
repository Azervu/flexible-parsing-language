namespace FlexibleParsingLanguage.Parse;

internal partial class ParsingContext
{
    internal struct ParsingFocusEntry
    {
        internal ParsingConfigContext Config;
        internal List<object> Keys;
        internal List<object> Reads;
        internal bool MultiRead;
        internal object Write;
    }

    internal ParsingConfigContext ConfigRoot;
    internal object ReadRoot;
    internal IReadingModule ReadingModule;
    internal object WriteRoot;
    internal IWritingModule WritingModule;
    internal readonly Dictionary<int, List<ParsingFocusEntry>> Store;
    internal List<ParsingFocusEntry> Focus;
    private Type _activeType = null;
    private ModuleHandler _modules;

    public ParsingContext(
        IWritingModule writingModule,
        ModuleHandler modules,
        object readRoot,
        object writeRoot,
        ParsingConfigContext parsingConfig
    )
    {
        _modules = modules;
        ReadRoot = readRoot;
        WriteRoot = writeRoot;
        Focus = new List<ParsingFocusEntry> {
            new ParsingFocusEntry
            {
                Keys = new List<object> { null },
                Reads = new List<object> { readRoot },
                MultiRead = false,
                Write = writeRoot,
                Config = parsingConfig
            }
        };
        Store = new Dictionary<int, List<ParsingFocusEntry>> {
            { 1, Focus }
        };
        WritingModule = writingModule;
        ConfigRoot = parsingConfig;
    }

    internal void ToRootRead()
    {
        var root = Store[1][0];
        var result = new List<ParsingFocusEntry>();
        foreach (var f in Focus)
        {
            result.Add(new ParsingFocusEntry
            {
                Keys = root.Keys,
                Reads = root.Reads,
                MultiRead = f.MultiRead,
                Write = f.Write,
                Config = f.Config,
            });
        }

        Focus = result;
    }

    internal void ToRootWrite()
    {
        var root = Store[1][0];
        var result = new List<ParsingFocusEntry>();
        foreach (var f in Focus)
        {
            result.Add(new ParsingFocusEntry
            {
                Keys = root.Keys,
                Reads = f.Reads,
                MultiRead = f.MultiRead,
                Write = root.Write,
                Config = f.Config,
            });
        }
        Focus = result;
    }






    /*
internal void ReadConfig()
{
    var result = new List<ParsingFocusEntry>();
    foreach (var focusEntry in Focus)
    {
        var innerResult = new List<object>();
        foreach (var rr in focusEntry.Reads)
        {
            if (focusEntry.Config.Data.TryGetValue(rr, out var v))
                v = null;
            innerResult.Add(v);
        }
        result.Add(new ParsingFocusEntry
        {
            Keys = focusEntry.Keys,
            Reads = innerResult,
            MultiRead = focusEntry.MultiRead,
            Write = focusEntry.Write
        });
    }
    Focus = result;
}





internal void ReadConfigChangeContext(Func<IReadingModule, object, object> readFunc)
{
    var result = new List<ParsingFocusEntry>();
    foreach (var focusEntry in Focus)
    {
        var innerResult = new List<object>();
        foreach (var rr in focusEntry.Reads)
        {
            if (focusEntry.Config.Data.TryGetValue(rr, out var v))
                v = null;
            innerResult.Add(v);
        }
        result.Add(new ParsingFocusEntry
        {
            Keys = focusEntry.Keys,
            Reads = innerResult,
            MultiRead = focusEntry.MultiRead,
            Write = focusEntry.Write
        });
    }
    Focus = result;
}
*/












    internal void ReadAction(Func<IReadingModule, object, object> readFunc)
    {
        var result = new List<ParsingFocusEntry>();
        foreach (var focusEntry in Focus)
        {
            var keys = new List<object>();
            var innerResult = new List<object>();
            foreach (var rr in focusEntry.Reads)
            {
                UpdateReadModule(rr);
                var r2 = readFunc(ReadingModule, rr);
                keys.Add(rr);
                innerResult.Add(r2);
            }
            result.Add(new ParsingFocusEntry
            {
                Keys = keys,
                Reads = innerResult,
                MultiRead = focusEntry.MultiRead,
                Write = focusEntry.Write,
                Config = focusEntry.Config,
            });
        }
        Focus = result;
    }

    internal void ReadTransform(Func<ParsingConfigContext, object, object> readFunc)
    {
        var result = new List<ParsingFocusEntry>();
        foreach (var focusEntry in Focus)
        {
            var keys = new List<object>();
            var innerResult = new List<object>();
            foreach (var rr in focusEntry.Reads)
            {
                var r2 = readFunc(focusEntry.Config, rr);
                keys.Add(rr);
                innerResult.Add(r2);
            }
            result.Add(new ParsingFocusEntry
            {
                MultiRead = focusEntry.MultiRead,
                Config = focusEntry.Config,
                Keys = keys,
                Reads = innerResult,
                Write = focusEntry.Write
            });
        }
        Focus = result;
    }

    internal void ReadFlatten()
    {
        var result = new List<ParsingFocusEntry>();
        foreach (var focusEntry in Focus)
        {
            var keys = new List<object>();
            var innerResult = new List<object>();
            foreach (var read in focusEntry.Reads)
            {
                UpdateReadModule(read);
                foreach (var (k, v) in ReadingModule.Foreach(read))
                {
                    keys.Add(k);
                    innerResult.Add(v);
                }

            }
            result.Add(new ParsingFocusEntry
            {
                Keys = keys,
                Reads = innerResult,
                MultiRead = true,
                Write = focusEntry.Write,
                Config = focusEntry.Config,
            });
        }
        Focus = result;
    }

    internal void ReadName()
    {
        var result = new List<ParsingFocusEntry>();
        foreach (var focusEntry in Focus)
        {
            result.Add(new ParsingFocusEntry
            {
                Keys = focusEntry.Keys,
                Reads = focusEntry.Keys,
                MultiRead = focusEntry.MultiRead,
                Write = focusEntry.Write,
                Config = focusEntry.Config,
            });
        }
        Focus = result;
    }

    internal void WriteFlatten()
    {
        var result = new List<ParsingFocusEntry>();
        foreach (var focusEntry in Focus)
        {
            for (var i = 0; i < focusEntry.Reads.Count; i++)
            {
                var key = focusEntry.Keys?[i] ?? null;
                var value = focusEntry.Reads[i];
                var w = WritingModule.BlankMap();
                WritingModule.Append(focusEntry.Write, w);
                result.Add(new ParsingFocusEntry
                {
                    Keys = [key],
                    Reads = [value],
                    Write = w,
                    MultiRead = false,
                    Config = focusEntry.Config,
                });
            }
        }
        Focus = result;
    }

    internal void WriteFlattenArray()
    {
        var result = new List<ParsingFocusEntry>();
        foreach (var focusEntry in Focus)
        {
            foreach (var read in focusEntry.Reads)
            {
                var w = WritingModule.BlankArray();
                WritingModule.Append(focusEntry.Write, w);
                result.Add(new ParsingFocusEntry
                {
                    Keys = focusEntry.Keys,
                    Reads = [TransformRead(read)],
                    Write = w,
                    MultiRead = false,
                    Config = focusEntry.Config,
                });
            }
        }
        Focus = result;
    }

    internal void WriteFromRead(string acc)
    {
        foreach (var focusEntry in Focus)
        {
            //UpdateWriteModule(w);
            var r = focusEntry.MultiRead ? focusEntry.Reads.Select(TransformRead).ToList() : TransformRead(focusEntry.Reads[0]);
            WritingModule.Write(focusEntry.Write, acc, r);

        }
    }

    internal void WriteAddRead()
    {
        foreach (var focusEntry in Focus)
        {
            //UpdateWriteModule(w);
            if (focusEntry.MultiRead)
            {
                foreach (var r in focusEntry.Reads)
                    WritingModule.Append(focusEntry.Write, TransformRead(r));
                continue;
            }
            WritingModule.Append(focusEntry.Write, focusEntry.Reads[0]);
        }
    }

    internal void WriteTransform(Func<object, object> writeFunc)
    {
        var result = new List<ParsingFocusEntry>();
        foreach (var focusEntry in Focus)
        {
            result.Add(new ParsingFocusEntry
            {
                Reads = focusEntry.Reads,
                Write = writeFunc(focusEntry.Write),
                Keys = focusEntry.Keys,
                MultiRead = focusEntry.MultiRead,
                Config = focusEntry.Config,
            });
        }
        Focus = result;
    }

    internal void WriteAction(Func<IWritingModule, object, object> writeFunc)
    {
        var result = new List<ParsingFocusEntry>();
        foreach (var focusEntry in Focus)
        {
            result.Add(new ParsingFocusEntry
            {
                Reads = focusEntry.Reads,
                Write = writeFunc(WritingModule, focusEntry.Write),
                Keys = focusEntry.Keys,
                MultiRead = focusEntry.MultiRead,
                Config = focusEntry.Config,
            });
        }
        Focus = result;
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