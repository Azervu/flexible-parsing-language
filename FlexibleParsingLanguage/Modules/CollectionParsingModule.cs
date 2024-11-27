using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Modules
{
    internal class CollectionParsingModule : IReadingModule
    {
        public List<Type> HandledTypes => [typeof(IList), typeof(IDictionary)];

        public IEnumerable Foreach(object raw)
        {




            switch (raw)
            {
                case IList x:
                    return x;
                case IDictionary x:
                    return x;
            }
            return null;
        }

        public object Parse(object raw, string acc)
        {
            if (raw is IDictionary n)
                return n[acc];

            return null;
        }

        public object Parse(object raw, int acc)
        {
            if (raw is IList a)
                return a[acc];

            return null;
        }
    }
}
