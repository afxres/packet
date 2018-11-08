using System;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class ArrayConverter<T> : Converter<T[]>
    {
        internal static void Bytes(Allocator allocator, T[] value, Converter<T> converter)
        {
            if (value == null || value.Length == 0)
                return;

            if (converter.length == 0)
                for (var i = 0; i < value.Length; i++)
                    allocator.AppendValueExtend(converter, value[i]);
            else
                for (var i = 0; i < value.Length; i++)
                    converter.ToBytes(allocator, value[i]);
        }

        internal static T[] Value(ReadOnlySpan<byte> memory, Converter<T> converter)
        {
            if (memory.IsEmpty)
                return Array.Empty<T>();
            var definition = converter.length;
            if (definition == 0)
                return ListConverter<T>.Value(memory, converter).ToArray();

            var quotient = Math.DivRem(memory.Length, definition, out var reminder);
            if (reminder != 0)
                ThrowHelper.ThrowOverflow();
            var array = new T[quotient];
            for (var i = 0; i < quotient; i++)
                array[i] = converter.ToValue(memory.Slice(i * definition, definition));
            return array;
        }

        private readonly Converter<T> converter;

        public ArrayConverter(Converter<T> converter) : base(0) => this.converter = converter;

        public override void ToBytes(Allocator allocator, T[] value) => Bytes(allocator, value, converter);

        public override T[] ToValue(ReadOnlySpan<byte> memory) => Value(memory, converter);
    }
}
