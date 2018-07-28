using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal sealed class UnmanagedValueConverter<T> : Converter<T> where T : unmanaged
    {
        private static readonly unsafe bool origin = BitConverter.IsLittleEndian == UseLittleEndian || sizeof(T) == 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void UnsafeToBytes(ref byte location, T value)
        {
            fixed (byte* pointer = &location)
                UnsafeToBytes(pointer, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe void UnsafeToBytes(byte* pointer, T value)
        {
            if (origin)
                *(T*)pointer = value;
            else
                Endian.Swap<T>(pointer, (byte*)&value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe T UnsafeToValue(in byte location)
        {
            fixed (byte* pointer = &location)
                return UnsafeToValue(pointer);
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
            UnsafeToBytes(ref allocator.Allocate(sizeof(T)), value);
        }

        internal static unsafe T Value(ReadOnlyMemory<byte> memory)
        {
            if (memory.Length < sizeof(T))
                ThrowHelper.ThrowOverflow();
            return UnsafeToValue(in memory.Span[0]);
        }

        public unsafe UnmanagedValueConverter() : base(sizeof(T)) { }

        public override void ToBytes(Allocator allocator, T value) => Bytes(allocator, value);

        public override T ToValue(ReadOnlyMemory<byte> memory) => Value(memory);
    }
}
