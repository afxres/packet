using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal class _Enumerable : IEnumerable
    {
        internal readonly PacketReader _src = null;
        internal readonly IPacketConverter _con = null;

        internal _Enumerable(PacketReader source, IPacketConverter converter)
        {
            _src = source;
            _con = converter;
        }

        IEnumerator IEnumerable.GetEnumerator() => new _Enumerator(_src, _con);
    }

    internal sealed class _Enumerable<T> : _Enumerable, IEnumerable<T>
    {
        internal _Enumerable(PacketReader source, IPacketConverter converter) : base(source, converter) { }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            if (_con is IPacketConverter<T> con)
                return new _EnumeratorGeneric<T>(_src, con);
            return new _Enumerator<T>(_src, _con);
        }
    }
}
