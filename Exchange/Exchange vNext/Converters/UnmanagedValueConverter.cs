using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal sealed class UnmanagedValueConverter<T> : Converter<T> where T : unmanaged
    {
        private static readonly unsafe bool origin = BitConverter.IsLittleEndian == UseLittleEndian || sizeof(T) == 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void UnsafeToBytes(ref byte location, T value)
        {
            if (origin)
                Unsafe.Assign(ref location, value);
            else
                Endian.SwapAssign(ref location, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T UnsafeToValue(in byte location)
        {
            if (origin)
                return Unsafe.As<T>(in location);
            else
                return Endian.SwapAs<T>(in location);
        }

        internal static unsafe void Bytes(Allocator allocator, T value)
        {
            UnsafeToBytes(ref allocator.Allocate(sizeof(T)).Span[0], value);
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
