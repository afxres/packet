using System;

namespace Mikodev.Network
{
    internal sealed class _ConvertValue<T> : _ConvertBase<T>, IPacketConverter<T>
    {
        internal readonly int _len = 0;
        internal readonly Func<byte[], int, T> _val = null;

        internal _ConvertValue(Func<T, byte[]> bin, Func<byte[], int, T> val, int len) : base(bin)
        {
            _val = val;
            _len = len;
        }

        public int Length => _len;

        public object GetValue(byte[] buffer, int offset, int length) => _val.Invoke(buffer, offset);

        T IPacketConverter<T>.GetValue(byte[] buffer, int offset, int length) => _val.Invoke(buffer, offset);
    }
}
