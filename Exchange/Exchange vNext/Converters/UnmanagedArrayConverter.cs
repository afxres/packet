using Mikodev.Binary.Common;
using System;
using System.Runtime.InteropServices;

namespace Mikodev.Binary.Converters
{
    internal class UnmanagedArrayConverter<T> : ArrayConverter<T> where T : unmanaged
    {
        public override T[] ToArray(Span<byte> block)
        {
            if (block.IsEmpty)
                return Array.Empty<T>();
            var source = MemoryMarshal.Cast<byte, T>(block);
            var target = new T[source.Length];
            source.CopyTo(target);
            return target;
        }

        public override void ToBytes(Allocator allocator, Span<T> array)
        {
            if (array.IsEmpty)
                return;
            var source = MemoryMarshal.Cast<T, byte>(array);
            var target = allocator.Allocate(source.Length);
            source.CopyTo(target);
        }
    }
}
