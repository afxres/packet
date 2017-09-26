using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal class _GenericEnumerator<T> : _EnumeratorBase, IEnumerator, IEnumerator<T>
    {
        internal readonly IPacketConverter<T> _con = null;

        internal T _cur = default(T);

        internal _GenericEnumerator(PacketReader source, IPacketConverter<T> converter) : base(source, converter) => _con = converter;

        object IEnumerator.Current => _cur;

        T IEnumerator<T>.Current => _cur;

        public bool MoveNext()
        {
            if (_idx >= _max)
                return false;
            var val = _bit;
            var idx = _idx;
            if ((_bit < 1 && _buf._Read(ref idx, out val, _max) == false) || (_bit > 0 && idx + val > _max))
                throw new PacketException(PacketError.Overflow);
            _cur = _con.GetValue(_buf, idx, val);
            _idx = idx + val;
            return true;
        }

        public void Reset()
        {
            _idx = _off;
            _cur = default(T);
        }
    }
}
