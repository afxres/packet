using System;
using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal class _Enumerator : IEnumerator, IDisposable
    {
        internal readonly int _bit = 0;
        internal readonly int _off = 0;
        internal readonly int _max = 0;
        internal readonly byte[] _buf = null;
        internal readonly IPacketConverter _con = null;

        internal int _idx = 0;
        internal object _cur = null;

        internal _Enumerator(PacketReader source, IPacketConverter converter)
        {
            _buf = source._buf;
            _off = source._off;
            _idx = source._off;
            _max = source._off + source._len;
            _bit = converter.Length ?? -1;
            _con = converter;
        }

        object IEnumerator.Current => _cur;

        void IDisposable.Dispose() { }

        public bool MoveNext()
        {
            if (_idx >= _max)
                return false;

            if (_bit < 1)
            {
                if (_buf._Read(ref _idx, out var val, _max) == false)
                    return false;
                _cur = _con.GetValue(_buf, _idx, val);
                _idx += val;
                return true;
            }

            if (_idx + _bit > _max)
                return false;
            _cur = _con.GetValue(_buf, _idx, _bit);
            _idx += _bit;
            return true;
        }

        public void Reset()
        {
            _idx = _off;
            _cur = null;
        }
    }

    internal class _Enumerator<T> : _Enumerator, IEnumerator<T>
    {
        internal _Enumerator(PacketReader reader, IPacketConverter converter) : base(reader, converter) { }

        T IEnumerator<T>.Current => (_cur != null) ? (T)_cur : default(T);
    }
}
