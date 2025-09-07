using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleParsingLanguage.Modules
{
    internal partial class WriteRoot
    {
        internal List<object> List = new();
        internal Dictionary<string, object> Dictionary = new();
    }

    internal partial class WriteRoot : IList
    {
        public object? this[int index] { get => ((IList)List)[index]; set => ((IList)List)[index] = value; }

        public bool IsFixedSize => ((IList)List).IsFixedSize;

        public bool IsReadOnly => ((IList)List).IsReadOnly;

        public int Count => ((ICollection)List).Count;

        public bool IsSynchronized => ((ICollection)List).IsSynchronized;

        public object SyncRoot => ((ICollection)List).SyncRoot;

        public int Add(object? value)
        {
            return ((IList)List).Add(value);
        }

        public void Clear()
        {
            ((IList)List).Clear();
        }

        public bool Contains(object? value)
        {
            return ((IList)List).Contains(value);
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)List).CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)List).GetEnumerator();
        }

        public int IndexOf(object? value)
        {
            return ((IList)List).IndexOf(value);
        }

        public void Insert(int index, object? value)
        {
            ((IList)List).Insert(index, value);
        }

        public void Remove(object? value)
        {
            ((IList)List).Remove(value);
        }

        public void RemoveAt(int index)
        {
            ((IList)List).RemoveAt(index);
        }
    }


    internal partial class WriteRoot : IDictionary
    {
        public object? this[object key] { get => ((IDictionary)Dictionary)[key]; set => ((IDictionary)Dictionary)[key] = value; }

        public ICollection Keys => ((IDictionary)Dictionary).Keys;

        public ICollection Values => ((IDictionary)Dictionary).Values;

        public void Add(object key, object? value)
        {
            ((IDictionary)Dictionary).Add(key, value);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return ((IDictionary)Dictionary).GetEnumerator();
        }
    }

}
