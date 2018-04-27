using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class EnumerableAdapter<T> : IEnumerable<T>
    {
        private readonly PacketReader reader;
        private readonly Info info;
        private readonly int level;

        internal EnumerableAdapter(PacketReader reader, int level, Info info)
        {
            this.reader = reader;
            this.level = level;
            this.info = info;
        }

        private IEnumerator<T> Enumerator()
        {
            var lst = reader.GetList();
            for (int i = 0; i < lst.Count; i++)
                yield return (T)lst[i].GetValueMatch(typeof(T), level, info);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => Enumerator();

        IEnumerator IEnumerable.GetEnumerator() => Enumerator();
    }
}
