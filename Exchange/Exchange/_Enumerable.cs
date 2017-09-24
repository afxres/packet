using System;
using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal class _Enumerable : IEnumerable
    {
        internal readonly PacketReader _src = null;
        internal readonly PacketConverter _con = null;

        internal _Enumerable(PacketReader source, Type type)
        {
            _src = source;
            _con = _Caches.Converter(type, source._con, false);
        }

        IEnumerator IEnumerable.GetEnumerator() => new _Enumerator(_src, _con);
    }

    internal class _Enumerable<T> : _Enumerable, IEnumerable<T>
    {
        internal _Enumerable(PacketReader reader) : base(reader, typeof(T)) { }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new _Enumerator<T>(_src, _con);
    }
}
