using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal class _Enumerator : IEnumerator
    {
        internal _Element _element;
        internal object _current = null;
        internal readonly IPacketConverter _converter = null;

        internal _Enumerator(PacketReader source, IPacketConverter converter)
        {
            _element = new _Element(source._element);
            _converter = converter;
        }

        object IEnumerator.Current => _current;

        public bool MoveNext()
        {
            if (_element.End())
                return false;
            _current = _element.Next(_converter);
            return true;
        }

        public void Reset()
        {
            _element._index = _element._offset;
            _current = null;
        }
    }

    internal sealed class _Enumerator<T> : _Enumerator, IEnumerator<T>
    {
        internal _Enumerator(PacketReader reader, IPacketConverter converter) : base(reader, converter) { }

        T IEnumerator<T>.Current => (_current != null) ? (T)_current : default(T);

        public void Dispose() { }
    }
}
