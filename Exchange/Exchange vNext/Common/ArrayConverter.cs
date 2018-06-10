using System;

namespace Mikodev.Binary.Common
{
    public abstract class ArrayConverter : Converter
    {
        internal abstract Type ArrayType { get; }
    }

    public abstract class ArrayConverter<T> : ArrayConverter
    {
        internal sealed override Type ArrayType => typeof(T[]);

        public abstract T[] ToArray(Span<byte> bytes);

        public abstract void ToBytes(Allocator allocator, Span<T> array);
    }
}
