using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Parse;
internal class ModuleHandler
{
    private List<IReadingModule> _modules;

    private Dictionary<Type, IReadingModule> _moduleLookup = new Dictionary<Type, IReadingModule>();

    internal ModuleHandler(List<IReadingModule> modules)
    {
        modules.Reverse();
        _modules = modules;
    }

    internal void Register(IReadingModule module)
    {
        _modules.Add(module);
    }

    public IReadingModule LookupModule(Type t)
    {
        if (_moduleLookup.TryGetValue(t, out var m))
            return m;

        foreach (var m2 in _modules)
        {
            foreach (var mt in m2.HandledTypes)
            {
                if (mt.IsAssignableFrom(t))
                {
                    _moduleLookup.Add(t, m2);
                    return m2;
                }
            }
        }
        return null;
    }
}
