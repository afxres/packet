using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal sealed class UnmanagedArrayConverter<T> : Converter<T[]> where T : unmanaged
    {
        public UnmanagedArrayConverter() : base(0) { }

        public override void ToBytes(Allocator allocator, T[] array)
        {
            if (array == null || array.Length == 0)
                return;
            var block = allocator.Allocate(array.Length * Unsafe.SizeOf<T>());
            Unsafe.CopyBlockUnaligned(ref block.Location, ref Unsafe.As<T, byte>(ref array[0]), (uint)block.Length);
        }

        public override T[] ToValue(Block block)
        {
            if (block.IsEmpty)
                return Array.Empty<T>();
            if (block.Length % Unsafe.SizeOf<T>() != 0)
                throw new OverflowException();
            var target = new T[block.Length / Unsafe.SizeOf<T>()];
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref target[0]), ref block.Location, (uint)block.Length);
            return target;
        }
    }
}
