using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal sealed class UnmanagedValueConverter<T> : Converter<T> where T : unmanaged
    {
        private static readonly unsafe bool origin = BitConverter.IsLittleEndian == UseLittleEndian || sizeof(T) == 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void UnsafeToBytes(byte* pointer, T value)
        {
            if (origin)
                *(T*)pointer = value;
            else
                Endian.Swap<T>(pointer, (byte*)&value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe T UnsafeToValue(byte* pointer)
        {
            T result;
            if (origin)
                result = *(T*)pointer;
            else
                Endian.Swap<T>((byte*)&result, pointer);
            return result;
        }

        internal static unsafe void Bytes(Allocator allocator, T value)
        {
            fixed (byte* dstptr = allocator.Allocate(sizeof(T)))
                UnsafeToBytes(dstptr, value);
        }

        internal static unsafe T Value(ReadOnlySpan<byte> memory)
        {
            if (memory.Length < sizeof(T))
                ThrowHelper.ThrowOverflow();
            fixed (byte* srcptr = memory)
                return UnsafeToValue(srcptr);
        }

        public unsafe UnmanagedValueConverter() : base(sizeof(T)) { }

        public override void ToBytes(Allocator allocator, T value) => Bytes(allocator, value);

        public override T ToValue(ReadOnlySpan<byte> memory) => Value(memory);
    }
}
