using System;

namespace Mikodev.Network.Converters
{
    internal sealed class UnmanagedArrayConverter<T> : PacketConverter<T[]> where T : unmanaged
    {
        private static readonly unsafe bool origin = BitConverter.IsLittleEndian == PacketConvert.UseLittleEndian || sizeof(T) == 1;

        internal static unsafe byte[] ToBytes(T[] source)
        {
            if (source == null || source.Length == 0)
                return Empty.Array<byte>();
            var targetLength = source.Length * sizeof(T);
            var target = new byte[targetLength];
            if (origin)
                Unsafe.Copy(ref target[0], in source[0], targetLength);
            else
                Endian.SwapCopy<T>(ref target[0], in source[0], targetLength);
            return target;
        }

        internal static unsafe T[] ToValue(byte[] buffer, int offset, int length)
        {
            if (length == 0)
                return Empty.Array<T>();
            if (buffer == null || length < 0 || offset < 0 || buffer.Length - offset < length || (length % sizeof(T)) != 0)
                throw PacketException.Overflow();
            var target = new T[length / sizeof(T)];
            if (origin)
                Unsafe.Copy(ref target[0], in buffer[offset], length);
            else
                Endian.SwapCopy<T>(ref target[0], in buffer[offset], length);
            return target;
        }

        public UnmanagedArrayConverter() : base(0) { }

        public override byte[] GetBytes(T[] value) => ToBytes(value);

        public override byte[] GetBytes(object value) => ToBytes((T[])value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override T[] GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
