using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class TupleListBuilder<TK, TV> : DictionaryAbstract<TK, TV>
    {
        internal readonly List<Tuple<TK, TV>> tuples = new List<Tuple<TK, TV>>();

        internal override void Add(TK key, TV value)
        {
            tuples.Add(new Tuple<TK, TV>(key, value));
        }
    }
}
