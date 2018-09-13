using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Network.Converters
{
    internal sealed class UnmanagedValueConverter<T> : PacketConverter<T> where T : unmanaged
    {
        private static readonly unsafe bool origin = BitConverter.IsLittleEndian == PacketConvert.UseLittleEndian || sizeof(T) == 1;

        internal static unsafe byte[] ToBytes(T value)
        {
            var buffer = new byte[sizeof(T)];
            UnsafeToBytes(ref buffer[0], value);
            return buffer;
        }

        internal static unsafe T ToValue(byte[] buffer, int offset, int length)
        {
            if (buffer == null || offset < 0 || length < sizeof(T) || buffer.Length - offset < length)
                throw PacketException.Overflow();
            return UnsafeToValue(ref buffer[offset]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void UnsafeToBytes(ref byte location, T value)
        {
            fixed (byte* pointer = &location)
            {
                if (origin)
                    *(T*)pointer = value;
                else
                    Endian.Swap<T>(pointer, (byte*)&value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe T UnsafeToValue(ref byte location)
        {
            T result;
            fixed (byte* pointer = &location)
            {
                if (origin)
                    result = *(T*)pointer;
                else
                    Endian.Swap<T>((byte*)&result, pointer);
            }
            return result;
        }

        public unsafe UnmanagedValueConverter() : base(sizeof(T)) { }

        public override byte[] GetBytes(T value) => ToBytes(value);

        public override byte[] GetBytes(object value) => ToBytes((T)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override T GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
