using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class _EnumerableReader<T> : IEnumerable<T>
    {
        private readonly PacketReader _reader;
        private readonly int _level;

        internal _EnumerableReader(PacketReader reader, int level)
        {
            _reader = reader;
            _level = level;
        }

        private static IEnumerator<T> _Enumerable(PacketReader[] arr, int lvl)
        {
            for (int i = 0; i < arr.Length; i++)
                yield return (T)arr[i].GetValue(typeof(T), lvl);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _Enumerable(_reader.GetArray(), _level);

        IEnumerator IEnumerable.GetEnumerator() => _Enumerable(_reader.GetArray(), _level);
    }
}
