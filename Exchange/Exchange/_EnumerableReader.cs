using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class _EnumerableReader<T> : IEnumerable<T>
    {
        private readonly PacketReader _rea;
        private readonly int _lev;

        internal _EnumerableReader(PacketReader reader, int level)
        {
            _rea = reader;
            _lev = level;
        }

        private static IEnumerator<T> _Enumerable(PacketReader[] arr, int lvl)
        {
            for (int i = 0; i < arr.Length; i++)
                yield return (T)arr[i].GetValue(typeof(T), lvl);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _Enumerable(_rea.GetArray(), _lev);

        IEnumerator IEnumerable.GetEnumerator() => _Enumerable(_rea.GetArray(), _lev);
    }
}
