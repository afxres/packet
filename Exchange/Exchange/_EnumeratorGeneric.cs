using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal class _GenericEnumerator<T> : IEnumerator, IEnumerator<T>
    {
        internal _Span _spa;
        internal T _cur = default(T);
        internal readonly IPacketConverter<T> _con = null;

        internal _GenericEnumerator(PacketReader source, IPacketConverter<T> converter)
        {
            _spa = source._spa;
            _con = converter;
        }

        object IEnumerator.Current => _cur;

        T IEnumerator<T>.Current => _cur;

        public bool MoveNext()
        {
            if (_spa._Next(_con.Length, false, out var idx, out var len) == false)
                return false;
            _cur = _con.GetValue(_spa._buf, idx, len);
            _spa._idx = idx + len;
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
