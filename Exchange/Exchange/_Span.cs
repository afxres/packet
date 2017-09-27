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

        internal _Span(_Span span)
        {
            this = span;
            _idx = span._off;
        }

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

        internal bool _Over() => _idx >= _max;

        internal void _Next(int? bit, Action<int, int> act)
        {
            var idx = _idx;
            var len = bit ?? -1;
            if ((bit.HasValue && idx + len > _max) || (bit.HasValue == false && _buf._Read(_max, ref idx, out len) == false))
                throw new PacketException(PacketError.Overflow);
            act.Invoke(idx, len);
            _idx = idx + len;
        }
    }
}
