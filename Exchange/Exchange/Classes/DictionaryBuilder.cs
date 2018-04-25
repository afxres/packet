using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class DictionaryBuilder<TK, TV> : DictionaryAbstract<TK, TV>
    {
        internal readonly Dictionary<TK, TV> dictionary = new Dictionary<TK, TV>();

        internal override void Add(TK key, TV value)
        {
            dictionary.Add(key, value);
        }
    }
}
