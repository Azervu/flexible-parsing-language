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

    private List<int>? _stack;
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
        _stack = [1];
        WritingModule = writingModule;

    }


    internal void ToRoot()
    {
        _stack = [_stack[0]];
    }

    internal void ReadAction(Func<IReadingModule, object, object> readFunc)
    {
        var readIt = _stack.Last();
        var readResult = new List<(object, object)>();

        var debugA = JsonSerializer.Serialize(readIt, new JsonSerializerOptions { WriteIndented = true });

        foreach (var (r, w) in Focus)
        {
            UpdateReadModule(r);
            var r2 = readFunc(ReadingModule, r);
            readResult.Add((r2, w));
        }

        //_stack[_stack.Count - 1] = readResult;

        var debugB = JsonSerializer.Serialize(readResult, new JsonSerializerOptions { WriteIndented = true });
    }

    internal void ReadFlatten()
    {
        //var debugB = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true });
        var readResult = new List<(object, object)>();
        foreach (var (read, write) in Focus)
        {
            UpdateReadModule(read);
            foreach (var rr in ReadingModule.Foreach(read))
                readResult.Add((rr, write));
        }
        Focus = readResult;

        //var debugC = JsonSerializer.Serialize(readResult, new JsonSerializerOptions { WriteIndented = true });
        //_stack.Add(readResult);
    }

    internal void WriteAction(Func<IWritingModule, object, object> writeFunc)
    {
        var readResult = new List<(object, object)>();
        foreach (var (r, w) in Focus)
        {
            //UpdateReadModule(r);
            var w2 = writeFunc(WritingModule, r);
            readResult.Add((r, w2));
        }
        //_stack[_stack.Count - 1] = readResult;
    }

    internal void UnbranchRead()
    {
        _stack.RemoveAt(_stack.Count - 1);
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
