using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Network.Converters
{
    internal sealed class UnmanagedArrayConverter<T> : PacketConverter<T[]> where T : unmanaged
    {
        private static readonly bool reverse = BitConverter.IsLittleEndian != PacketConvert.UseLittleEndian && Unsafe.SizeOf<T>() != 1;

        internal static byte[] ToBytes(T[] source)
        {
            if (source == null || source.Length == 0)
                return Extension.EmptyArray<byte>();
            var targetLength = source.Length * Unsafe.SizeOf<T>();
            var target = new byte[targetLength];
            Unsafe.CopyBlockUnaligned(ref target[0], ref Unsafe.As<T, byte>(ref source[0]), (uint)targetLength);
            if (reverse)
                Extension.ReverseEndiannessExplicitly<T>(target);
            return target;
        }

        internal static T[] ToValue(byte[] buffer, int offset, int length)
        {
            if (length == 0)
                return Extension.EmptyArray<T>();
            if (buffer == null || length < 0 || offset < 0 || buffer.Length - offset < length || (length % Unsafe.SizeOf<T>()) != 0)
                throw PacketException.Overflow();
            var target = new T[length / Unsafe.SizeOf<T>()];
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref target[0]), ref buffer[offset], (uint)length);
            if (reverse)
                Extension.ReverseEndianness(target);
            return target;
        }

        public UnmanagedArrayConverter() : base(0) { }

        public override byte[] GetBytes(T[] value) => ToBytes(value);

        public override byte[] GetBytes(object value) => ToBytes((T[])value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override T[] GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
