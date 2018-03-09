using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal class _Enumerable : IEnumerable
    {
        internal readonly PacketReader _reader = null;
        internal readonly IPacketConverter _converter = null;

        internal _Enumerable(PacketReader source, IPacketConverter converter)
        {
            _reader = source;
            _converter = converter;
        }

        IEnumerator IEnumerable.GetEnumerator() => new _Enumerator(_reader, _converter);
    }

    internal sealed class _Enumerable<T> : _Enumerable, IEnumerable<T>
    {
        internal _Enumerable(PacketReader source, IPacketConverter converter) : base(source, converter) { }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            if (_converter is IPacketConverter<T> con)
                return new _EnumeratorGeneric<T>(_reader, con);
            return new _Enumerator<T>(_reader, _converter);
        }
    }
}
