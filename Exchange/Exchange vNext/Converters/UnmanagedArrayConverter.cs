using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal sealed class UnmanagedArrayConverter<T> : Converter<T[]> where T : unmanaged
    {
        private static readonly bool origin = BitConverter.IsLittleEndian == UseLittleEndian || Unsafe.SizeOf<T>() == 1;

        public UnmanagedArrayConverter() : base(0) { }

        public override void ToBytes(Allocator allocator, T[] array)
        {
            if (array == null || array.Length == 0)
                return;
            var memory = allocator.Allocate(array.Length * Unsafe.SizeOf<T>());
            var span = memory.Span;
            if (origin)
                Unsafe.CopyBlockUnaligned(ref span[0], ref Unsafe.As<T, byte>(ref array[0]), (uint)span.Length);
            else
                Endian.SwapRange<T>(ref span[0], ref Unsafe.As<T, byte>(ref array[0]), span.Length);
        }

        public override T[] ToValue(Memory<byte> memory)
        {
            if (memory.IsEmpty)
                return Array.Empty<T>();
            var span = memory.Span;
            var quotient = Math.DivRem(span.Length, Unsafe.SizeOf<T>(), out var remainder);
            if (remainder != 0)
                ThrowHelper.ThrowOverflow();
            var target = new T[quotient];
            if (origin)
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref target[0]), ref span[0], (uint)span.Length);
            else
                Endian.SwapRange<T>(ref Unsafe.As<T, byte>(ref target[0]), ref span[0], span.Length);
            return target;
        }
    }
}
