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

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            var lst = _reader._GetItemList();
            foreach (var i in lst)
                yield return (T)i._GetValue(typeof(T), _level);
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            var lst = _reader._GetItemList();
            foreach (var i in lst)
                yield return i._GetValue(typeof(T), _level);
            yield break;
        }
    }
}
