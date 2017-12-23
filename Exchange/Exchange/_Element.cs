using System;

namespace Mikodev.Network
{
    internal struct _Element
    {
        internal readonly byte[] _buf;
        internal readonly int _off;
        internal readonly int _len;
        internal readonly int _max;
        internal int _idx;

        internal _Element(_Element ele)
        {
            this = ele;
            _idx = ele._off;
        }

        internal _Element(byte[] buffer)
        {
            _buf = buffer ?? throw new ArgumentNullException();
            _off = 0;
            _idx = 0;
            _len = buffer.Length;
            _max = buffer.Length;
        }

        internal _Element(byte[] buffer, int offset, int length)
        {
            _buf = buffer ?? throw new ArgumentNullException();
            if (offset < 0 || length < 0 || buffer.Length - offset < length)
                throw new ArgumentOutOfRangeException();
            _off = offset;
            _idx = offset;
            _len = length;
            _max = offset + length;
        }

        internal bool _End() => _idx >= _max;

        internal bool _Any() => _idx < _max;

        internal object _Next(IPacketConverter con)
        {
            var idx = _idx;
            var len = con.Length;
            if ((len > 0 && idx + len > _max) || (len < 1 && _buf._Read(_max, ref idx, out len) == false))
                throw new PacketException(PacketError.Overflow);
            var res = con._GetValueWrapErr(_buf, idx, len, false);
            _idx = idx + len;
            return res;
        }

        internal T _Next<T>(IPacketConverter con)
        {
            var idx = _idx;
            var len = con.Length;
            if ((len > 0 && idx + len > _max) || (len < 1 && _buf._Read(_max, ref idx, out len) == false))
                throw new PacketException(PacketError.Overflow);
            var res = con._GetValueWrapErr<T>(_buf, idx, len, false);
            _idx = idx + len;
            return res;
        }
    }
}
