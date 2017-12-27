using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal class _Enumerator : IEnumerator
    {
        internal _Element _spa;
        internal object _cur = null;
        internal readonly IPacketConverter _con = null;


        internal _Enumerator(PacketReader source, IPacketConverter converter)
        {
            _spa = new _Element(source._spa);
            _con = converter;
        }

        object IEnumerator.Current => _cur;

        public bool MoveNext()
        {
            if (_spa.End())
                return false;
            _cur = _spa.Next(_con);
            return true;
        }

        public void Reset()
        {
            _spa._idx = _spa._off;
            _cur = null;
        }
    }

    internal class _Enumerator<T> : _Enumerator, IEnumerator<T>
    {
        internal _Enumerator(PacketReader reader, IPacketConverter converter) : base(reader, converter) { }

        T IEnumerator<T>.Current => (_cur != null) ? (T)_cur : default(T);

        public void Dispose() { }
    }
}
