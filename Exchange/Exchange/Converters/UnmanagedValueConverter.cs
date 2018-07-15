using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Network.Converters
{
    internal sealed class UnmanagedValueConverter<T> : PacketConverter<T> where T : unmanaged
    {
        private static readonly bool origin = BitConverter.IsLittleEndian == PacketConvert.UseLittleEndian || Unsafe.SizeOf<T>() == 1;

        internal static byte[] ToBytes(T value)
        {
            var buffer = new byte[Unsafe.SizeOf<T>()];
            UnsafeToBytes(ref buffer[0], value);
            return buffer;
        }

        internal static T ToValue(byte[] buffer, int offset, int length)
        {
            if (buffer == null || offset < 0 || length < Unsafe.SizeOf<T>() || buffer.Length - offset < length)
                throw PacketException.Overflow();
            return UnsafeToValue(ref buffer[offset]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void UnsafeToBytes(ref byte location, T value)
        {
            if (origin)
                Unsafe.WriteUnaligned(ref location, value);
            else
                Endian.Swap<T>(ref location, ref Unsafe.As<T, byte>(ref value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T UnsafeToValue(ref byte location)
        {
            if (origin)
                return Unsafe.ReadUnaligned<T>(ref location);
            var result = default(T);
            Endian.Swap<T>(ref Unsafe.As<T, byte>(ref result), ref location);
            return result;
        }

        public UnmanagedValueConverter() : base(Unsafe.SizeOf<T>()) { }

        public override byte[] GetBytes(T value) => ToBytes(value);

        public override byte[] GetBytes(object value) => ToBytes((T)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override T GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
