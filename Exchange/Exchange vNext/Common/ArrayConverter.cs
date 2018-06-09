using System;

namespace Mikodev.Binary.Common
{
    public abstract class ArrayConverter<T> : Converter
    {
        public abstract T[] ToArray(Span<byte> bytes);

        public abstract void ToBytes(Allocator allocator, Span<T> array);
    }
}
