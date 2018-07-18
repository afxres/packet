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
            var block = allocator.Allocate(array.Length * Unsafe.SizeOf<T>());
            if (origin)
                Unsafe.CopyBlockUnaligned(ref block[0], ref Unsafe.As<T, byte>(ref array[0]), (uint)block.Length);
            else
                Endian.SwapRange<T>(ref block[0], ref Unsafe.As<T, byte>(ref array[0]), block.Length);
        }

        public override T[] ToValue(Block block)
        {
            if (block.Length == 0)
                return Empty.Array<T>();
            var quotient = Math.DivRem(block.Length, Unsafe.SizeOf<T>(), out var remainder);
            if (remainder != 0)
                ThrowHelper.ThrowOverflow();
            var target = new T[quotient];
            if (origin)
                Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref target[0]), ref block[0], (uint)block.Length);
            else
                Endian.SwapRange<T>(ref Unsafe.As<T, byte>(ref target[0]), ref block[0], block.Length);
            return target;
        }
    }
}
