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
            var arr = reader.GetArray();
            for (int i = 0; i < arr.Length; i++)
                yield return (T)arr[i].GetValueMatch(typeof(T), level, info);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => Enumerator();

        IEnumerator IEnumerable.GetEnumerator() => Enumerator();
    }
}
