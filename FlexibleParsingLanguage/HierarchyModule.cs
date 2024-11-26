using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage
{
    public class HierarchyModule<T>
    {
        public T Values;

        public List<HierarchyModule<T>> Children;
    }
}
