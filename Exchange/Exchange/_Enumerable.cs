using System;
using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal class _Enumerable : IEnumerable
    {
        internal readonly PacketReader _src = null;
        internal readonly IPacketConverter _con = null;

        internal _Enumerable(PacketReader source, Type type)
        {
            _src = source;
            _con = _Caches.Converter(source._cvt, type, false);
        }

        IEnumerator IEnumerable.GetEnumerator() => new _Enumerator(_src, _con);
    }

    internal class _Enumerable<T> : _Enumerable, IEnumerable<T>
    {
        internal _Enumerable(PacketReader reader) : base(reader, typeof(T)) { }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            if (_con is IPacketConverter<T> con)
                return new _EnumeratorGeneric<T>(_src, con);
            return new _Enumerator<T>(_src, _con);
        }
    }
}
