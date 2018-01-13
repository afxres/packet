using System;

namespace Mikodev.Network
{
    internal sealed class _ConvertBase<T>
    {
        internal readonly Func<T, byte[]> _bin = null;

        internal _ConvertBase(Func<T, byte[]> bin) => _bin = bin;

        public byte[] GetBytes(object value) => _bin.Invoke((T)value);

        public byte[] GetBytes(T value) => _bin.Invoke(value);
    }
}
