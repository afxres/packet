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
            var list = this.reader.GetList();
            for (var i = 0; i < list.Count; i++)
                yield return (T)list[i].GetValueMatch(typeof(T), this.level, this.info);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.Enumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.Enumerator();
    }
}
