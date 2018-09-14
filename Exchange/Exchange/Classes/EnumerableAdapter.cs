using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class EnumerableAdapter<T> : IEnumerable<T>
    {
        private readonly PacketReader reader;

        private readonly Info info;

        private readonly int level;

        internal EnumerableAdapter(PacketReader reader, Info info, int level)
        {
            this.reader = reader;
            this.info = info;
            this.level = level;
        }

        private IEnumerator<T> Enumerator()
        {
            var list = reader.GetList();
            for (int i = 0; i < list.Count; i++)
                yield return (T)list[i].GetValueMatch(typeof(T), level, info);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => Enumerator();

        IEnumerator IEnumerable.GetEnumerator() => Enumerator();
    }
}
