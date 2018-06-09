using System.Collections.Generic;

namespace Mikodev.Binary.Common
{
    public abstract class DictionaryConverter : Converter { }

    public abstract class DictionaryConverter<TDictionary, TKey, TValue> : DictionaryConverter where TDictionary : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        public abstract TDictionary ToDictionary(IEnumerable<KeyValuePair<TKey, TValue>> enumerable);
    }
}
