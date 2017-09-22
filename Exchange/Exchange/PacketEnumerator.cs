using System;
using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal class PacketEnumerator : IEnumerable, IEnumerator, IDisposable
    {
        internal readonly int _bit = 0;
        internal readonly int _off = 0;
        internal readonly int _max = 0;
        internal readonly byte[] _buf = null;
        internal readonly PacketConverter _con = null;

        internal int _idx = 0;
        internal object _cur = null;

        internal PacketEnumerator(PacketReader reader, PacketConverter converter)
        {
            _buf = reader._buf;
            _off = reader._off;
            _idx = reader._off;
            _max = reader._off + reader._len;
            _bit = converter.Length ?? -1;
            _con = converter;
        }

        object IEnumerator.Current => _cur;

        IEnumerator IEnumerable.GetEnumerator() => this;

        public void Dispose() { }

        public bool MoveNext()
        {
            if (_idx >= _max)
                return false;

            if (_bit < 1)
            {
                if (_buf._Read(ref _idx, out var val, _max) == false)
                    return false;
                _cur = _con.ToObject(_buf, _idx, val);
                _idx += val;
                return true;
            }

            if (_idx + _bit > _max)
                return false;
            _cur = _con.ToObject(_buf, _idx, _bit);
            _idx += _bit;
            return true;
        }

        public void Reset()
        {
            _idx = _off;
            _cur = null;
        }
    }

    internal class PacketEnumerator<T> : PacketEnumerator, IEnumerable<T>, IEnumerator<T>
    {
        internal PacketEnumerator(PacketReader reader, PacketConverter converter) : base(reader, converter) { }

        T IEnumerator<T>.Current => (_cur != null) ? (T)_cur : default(T);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => this;
    }
}
