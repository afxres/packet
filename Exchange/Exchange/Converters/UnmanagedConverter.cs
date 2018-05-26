using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Network.Converters
{
    internal class UnmanagedConverter<T> : PacketConverter<T> where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte[] ToBytes(T value)
        {
            var buffer = new byte[Unsafe.SizeOf<T>()];
            if (BitConverter.IsLittleEndian == Extension.UseLittleEndian)
                Unsafe.WriteUnaligned(ref buffer[0], value);
            else
                Unsafe.WriteUnaligned(ref buffer[0], Extension.ReverseEndianness(value));
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T ToValue(byte[] buffer, int offset, int length)
        {
            if (buffer == null || offset < 0 || length < Unsafe.SizeOf<T>() || buffer.Length - offset < length)
                throw PacketException.Overflow();
            return ToValueUnchecked(ref buffer[offset]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T ToValueUnchecked(ref byte location)
        {
            if (BitConverter.IsLittleEndian == Extension.UseLittleEndian)
                return Unsafe.ReadUnaligned<T>(ref location);
            else
                return Extension.ReverseEndianness(Unsafe.ReadUnaligned<T>(ref location));
        }

        public override int Length => Unsafe.SizeOf<T>();

        public override byte[] GetBytes(T value) => ToBytes(value);

        public override byte[] GetBytes(object value) => ToBytes((T)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override T GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
