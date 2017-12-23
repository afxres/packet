using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal class _EnumeratorGeneric<T> : IEnumerator, IEnumerator<T>
    {
        internal _Element _spa;
        internal T _cur = default(T);
        internal readonly IPacketConverter<T> _con = null;

        internal _EnumeratorGeneric(PacketReader source, IPacketConverter<T> converter)
        {
            _spa = new _Element(source._spa);
            _con = converter;
        }

        object IEnumerator.Current => _cur;

        T IEnumerator<T>.Current => _cur;

        public bool MoveNext()
        {
            if (_spa._End())
                return false;
            _cur = _spa._Next<T>(_con);
            return true;
        }

        public void Reset()
        {
            _spa._idx = _spa._off;
            _cur = default(T);
        }

        public void Dispose() { }
    }
}
