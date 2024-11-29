using FlexibleParsingLanguage.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage;

internal partial class ParsingContext
{
    internal object ReadRoot;
    internal IReadingModule ReadingModule;

    internal object WriteRoot;
    internal IWritingModule WritingModule;

    internal readonly Dictionary<int, IEnumerable<(object, object)>> Store;
    internal IEnumerable<(object, object)> Focus;

    private Type _activeType = null;
    private ModuleHandler _modules;

    public ParsingContext(
        IWritingModule writingModule,
        ModuleHandler modules,
        object readRoot,
        object writeRoot
    )
    {
        _modules = modules;
        ReadRoot = readRoot;
        WriteRoot = writeRoot;
        Focus = new List<(object, object)> { (readRoot, writeRoot) };
        Store = new Dictionary<int, IEnumerable<(object, object)>> {
            { 1, Focus }
        };
        WritingModule = writingModule;

    }


    internal void ToRoot()
    {
        Focus = Store[1];
    }



    internal void Action(Func<IWritingModule, (object, object), (object, object)> writeFunc)
    {
        var result = new List<(object, object)>();
        foreach (var x in Focus)
            result.Add(writeFunc(WritingModule, x));
        Focus = result;
    }





    internal void ReadAction(Func<IReadingModule, object, object> readFunc)
    {
        var readResult = new List<(object, object)>();

        foreach (var (r, w) in Focus)
        {
            UpdateReadModule(r);
            var r2 = readFunc(ReadingModule, r);
            readResult.Add((r2, w));
        }

        var debugB = JsonSerializer.Serialize(readResult, new JsonSerializerOptions { WriteIndented = true });
    }

    internal void ReadFlatten()
    {
        var result = new List<(object, object)>();
        foreach (var (read, write) in Focus)
        {
            UpdateReadModule(read);
            foreach (var rr in ReadingModule.Foreach(read))
                result.Add((rr, write));
        }
        Focus = result;
    }

    internal void WriteAction(Func<IWritingModule, object, object> writeFunc)
    {
        var result = new List<(object, object)>();
        foreach (var (r, w) in Focus)
        {
            //UpdateReadModule(r);
            var w2 = writeFunc(WritingModule, r);
            result.Add((r, w2));
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






}
