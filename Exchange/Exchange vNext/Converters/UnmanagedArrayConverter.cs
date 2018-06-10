using Mikodev.Binary.Common;
using System;
using System.Runtime.InteropServices;

namespace Mikodev.Binary.Converters
{
    internal class UnmanagedArrayConverter<T> : VariableValueConverter<T[]> where T : unmanaged
    {
        public override void ToBytes(Allocator allocator, T[] array)
        {
            if (array == null || array.Length == 0)
                return;
            var source = MemoryMarshal.Cast<T, byte>(array.AsSpan());
            var target = allocator.Allocate(source.Length);
            source.CopyTo(target);
        }

        public override T[] ToValue(Span<byte> block)
        {
            if (block.IsEmpty)
                return Array.Empty<T>();
            var source = MemoryMarshal.Cast<byte, T>(block);
            var target = new T[source.Length];
            source.CopyTo(target);
            return target;
        }
    }
}
