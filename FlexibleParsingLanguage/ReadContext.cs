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

internal class ReadContext
{
    internal object ReadRoot;
    internal object ReadHead;
    internal IReadingModule Module;

    private object _stackHead = null;
    private List<IEnumerable>? _readStack = new List<IEnumerable>();
    private Type _activeType = null;
    private ModuleHandler _modules;

    public ReadContext(ModuleHandler modules, object readRoot)
    {
        _modules = modules;
        ReadRoot = readRoot;
        ReadHead = readRoot;
    }

    internal void ReadAction(Func<IReadingModule, object, object> readFunc)
    {
        if (_readStack.Count == 0)
        {
            UpdateModule(ReadHead);
            ReadHead = readFunc(Module, ReadHead);
            return;
        }

        var readIt = _readStack.Last();
        var readResult = new List<object>();


        var debugA = JsonSerializer.Serialize(readIt, new JsonSerializerOptions { WriteIndented = true });


        foreach (var r in readIt)
        {
            UpdateModule(r);
            var r2 = readFunc(Module, r);
            readResult.Add(r2);
        }
        _readStack[_readStack.Count - 1] = readResult;

        var debugB = JsonSerializer.Serialize(readResult, new JsonSerializerOptions { WriteIndented = true });



        ReadHead = readResult;
    }

    internal void ReadFlatten()
    {
        if (_readStack.Count == 0)
        {
            _stackHead = ReadHead;
            var reads = Module.Foreach(_stackHead);

            ReadHead = reads;
            _readStack.Add(reads);
            return;
        }

        var read = _readStack.Last();

        var debugB = JsonSerializer.Serialize(read, new JsonSerializerOptions { WriteIndented = true });

        var readResult = new List<object>();
        foreach (var r in _readStack.Last())
        {
            UpdateModule(r);
            foreach (var rr in Module.Foreach(r))
            {
                readResult.Add(rr);
            }
        }


        var debugC = JsonSerializer.Serialize(readResult, new JsonSerializerOptions { WriteIndented = true });

        ReadHead = readResult;
        _readStack.Add(readResult);
    }

    internal void UnbranchRead()
    {
        _readStack.RemoveAt(_readStack.Count - 1);
        if (_readStack.Count == 0)
            ReadHead = _stackHead;
        else
            ReadHead = _readStack.Last();
    }

    private void UpdateModule(object obj)
    {
        var t = obj?.GetType() ?? typeof(void);
        if (t != _activeType)
        {
            _activeType = t;
            Module = _modules.LookupModule(t);
        }
    }


}
