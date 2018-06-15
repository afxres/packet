using Mikodev.Binary.Common;
using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal class UnmanagedArrayConverter<T> : ValueConverter<T[]> where T : unmanaged
    {
        public UnmanagedArrayConverter() : base(0) { }

        public override void ToBytes(Allocator allocator, T[] array)
        {
            if (array == null || array.Length == 0)
                return;
            var allocation = allocator.Allocate(array.Length * Unsafe.SizeOf<T>());
            Unsafe.CopyBlockUnaligned(ref allocation.Location, ref Unsafe.As<T, byte>(ref array[0]), (uint)allocation.Length);
        }

        public override T[] ToValue(Allocation allocation)
        {
            if (allocation.IsEmpty)
                return Array.Empty<T>();
            if (allocation.Length % Unsafe.SizeOf<T>() != 0)
                throw new OverflowException();
            var target = new T[allocation.Length / Unsafe.SizeOf<T>()];
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref target[0]), ref allocation.Location, (uint)allocation.Length);
            return target;
        }
    }
}
