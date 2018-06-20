using System;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class ArrayConverter<T> : Converter<T[]>
    {
        internal static void ToBytes(Allocator allocator, T[] value, Converter<T> converter)
        {
            if (value == null || value.Length == 0)
                return;

            if (converter.Length == 0)
            {
                int offset;
                var stream = allocator.stream;
                for (int i = 0; i < value.Length; i++)
                {
                    offset = stream.BeginModify();
                    converter.ToBytes(allocator, value[i]);
                    stream.EndModify(offset);
                }
            }
            else
            {
                for (int i = 0; i < value.Length; i++)
                {
                    converter.ToBytes(allocator, value[i]);
                }
            }
        }

        internal static T[] ToValue(Block block, Converter<T> converter)
        {
            if (block.IsEmpty)
                return Array.Empty<T>();
            if (converter.Length == 0)
                return ListConverter<T>.ToValue(block, converter).ToArray();

            var quotient = Math.DivRem(block.Length, converter.Length, out var reminder);
            if (reminder != 0)
                throw new OverflowException();
            var array = new T[quotient];
            for (int i = 0; i < quotient; i++)
                array[i] = converter.ToValue(new Block(block.Buffer, block.Offset + i * converter.Length, converter.Length));
            return array;
        }

        private readonly Converter<T> converter;

        public ArrayConverter(Converter<T> converter) : base(0) => this.converter = converter;

        public override void ToBytes(Allocator allocator, T[] value) => ToBytes(allocator, value, converter);

        public override T[] ToValue(Block block) => ToValue(block, converter);
    }
}
