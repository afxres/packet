using System;

namespace Mikodev.Network
{
    internal struct _Span
    {
        internal readonly byte[] _buf;
        internal readonly int _off;
        internal readonly int _len;
        internal readonly int _max;
        internal int _idx;

        internal _Span(byte[] buffer)
        {
            _buf = buffer ?? throw new ArgumentNullException();
            _off = 0;
            _idx = 0;
            _len = buffer.Length;
            _max = buffer.Length;
        }

        internal _Span(byte[] buffer, int offset, int length)
        {
            _buf = buffer ?? throw new ArgumentNullException();
            if (offset < 0 || length < 0 || buffer.Length - offset < length)
                throw new ArgumentOutOfRangeException();
            _off = offset;
            _idx = offset;
            _len = length;
            _max = offset + length;
        }

        internal bool _Next(int? bit, bool nothrow, out int idx, out int len)
        {
            idx = _idx;
            len = bit ?? -1;
            if (idx >= _max)
                return false;
            if ((bit.HasValue && idx + len <= _max))
                return true;
            if (bit.HasValue == false && _buf._Read(ref idx, out len, _max))
                return true;
            if (nothrow)
                return false;
            throw new PacketException(PacketError.Overflow);
        }
    }
}
