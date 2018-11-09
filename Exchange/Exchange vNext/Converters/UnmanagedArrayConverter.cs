using System;

namespace Mikodev.Binary.Converters
{
    internal sealed class UnmanagedArrayConverter<T> : Converter<T[]> where T : unmanaged
    {
        private static readonly unsafe bool origin = BitConverter.IsLittleEndian == UseLittleEndian || sizeof(T) == 1;

        public UnmanagedArrayConverter() : base(0) { }

        public override unsafe void ToBytes(ref Allocator allocator, T[] array)
        {
            if (array == null || array.Length == 0)
                return;
            var length = checked(array.Length * sizeof(T));
            fixed (byte* dstptr = allocator.Allocate(length))
            fixed (T* srcptr = &array[0])
            {
                if (origin)
                    Unsafe.Copy(dstptr, srcptr, length);
                else
                    Endian.SwapCopy(dstptr, srcptr, length);
            }
        }

        public override unsafe T[] ToValue(ReadOnlySpan<byte> memory)
        {
            if (memory.IsEmpty)
                return Array.Empty<T>();
            var limits = memory.Length;
            var quotient = Math.DivRem(limits, sizeof(T), out var remainder);
            if (remainder != 0)
                ThrowHelper.ThrowOverflow();
            var target = new T[quotient];
            fixed (byte* srcptr = memory)
            fixed (T* dstptr = &target[0])
            {
                if (origin)
                    Unsafe.Copy(dstptr, srcptr, limits);
                else
                    Endian.SwapCopy(dstptr, srcptr, limits);
            }
            return target;
        }
    }
}
