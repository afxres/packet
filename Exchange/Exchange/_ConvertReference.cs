using System;

namespace Mikodev.Network
{
    internal sealed class _ConvertReference<T> : _ConvertBase<T>, IPacketConverter<T>
    {
        internal readonly Func<byte[], int, int, T> _val = null;

        internal _ConvertReference(Func<T, byte[]> bin, Func<byte[], int, int, T> val) : base(bin) => _val = val;

        public int Length => 0;

        public object GetValue(byte[] buffer, int offset, int length) => _val.Invoke(buffer, offset, length);

        T IPacketConverter<T>.GetValue(byte[] buffer, int offset, int length) => _val.Invoke(buffer, offset, length);
    }
}
