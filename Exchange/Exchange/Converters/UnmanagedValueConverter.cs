using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Network.Converters
{
    internal sealed class UnmanagedValueConverter<T> : PacketConverter<T> where T : unmanaged
    {
        internal static readonly bool ReverseEndianness = BitConverter.IsLittleEndian != PacketConvert.UseLittleEndian && Unsafe.SizeOf<T>() != 1;

        internal static byte[] ToBytes(T value)
        {
            var buffer = new byte[Unsafe.SizeOf<T>()];
            ToBytesUnchecked(ref buffer[0], value);
            return buffer;
        }

        internal static T ToValue(byte[] buffer, int offset, int length)
        {
            if (buffer == null || offset < 0 || length < Unsafe.SizeOf<T>() || buffer.Length - offset < length)
                throw PacketException.Overflow();
            return ToValueUnchecked(ref buffer[offset]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ToBytesUnchecked(ref byte location, T value)
        {
            Unsafe.WriteUnaligned(ref location, (!ReverseEndianness) ? value : Extension.ReverseEndianness(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T ToValueUnchecked(ref byte location)
        {
            return (!ReverseEndianness)
                ? Unsafe.ReadUnaligned<T>(ref location)
                : Extension.ReverseEndianness(Unsafe.ReadUnaligned<T>(ref location));
        }

        public UnmanagedValueConverter() : base(Unsafe.SizeOf<T>()) { }

        public override byte[] GetBytes(T value) => ToBytes(value);

        public override byte[] GetBytes(object value) => ToBytes((T)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override T GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
