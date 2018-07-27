using System;

namespace Mikodev.Binary.Converters
{
    internal sealed class UnmanagedArrayConverter<T> : Converter<T[]> where T : unmanaged
    {
        private static readonly unsafe bool origin = BitConverter.IsLittleEndian == UseLittleEndian || sizeof(T) == 1;

        public UnmanagedArrayConverter() : base(0) { }

        public override unsafe void ToBytes(Allocator allocator, T[] array)
        {
            if (array == null || array.Length == 0)
                return;
            var length = array.Length * sizeof(T);
            var memory = allocator.Allocate(length);
            ref var target = ref memory.Span[0];
            if (origin)
                Unsafe.Copy(ref target, in array[0], length);
            else
                Endian.SwapCopy(ref target, in array[0], length);
        }

        public override unsafe T[] ToValue(ReadOnlyMemory<byte> memory)
        {
            if (memory.IsEmpty)
                return Array.Empty<T>();
            var span = memory.Span;
            var quotient = Math.DivRem(span.Length, sizeof(T), out var remainder);
            if (remainder != 0)
                ThrowHelper.ThrowOverflow();
            var target = new T[quotient];
            if (origin)
                Unsafe.Copy(ref target[0], in span[0], span.Length);
            else
                Endian.SwapCopy(ref target[0], in span[0], span.Length);
            return target;
        }
    }
}
