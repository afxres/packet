using Mikodev.Binary.Converters;
using System;
using System.Collections.Generic;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class ListConverter<T> : Converter<List<T>>
    {
        internal static void Bytes(Allocator allocator, List<T> value, Converter<T> converter)
        {
            if (value != null && value.Count != 0)
            {
                if (converter.Length == 0)
                {
                    int offset;
                    for (int i = 0; i < value.Count; i++)
                    {
                        offset = allocator.AnchorExtend();
                        converter.ToBytes(allocator, value[i]);
                        allocator.FinishExtend(offset);
                    }
                }
                else
                {
                    for (int i = 0; i < value.Count; i++)
                    {
                        converter.ToBytes(allocator, value[i]);
                    }
                }
            }
        }

        internal static List<T> Value(ReadOnlyMemory<byte> memory, Converter<T> converter)
        {
            if (memory.IsEmpty)
                return new List<T>(0);
            var definition = converter.Length;
            if (definition == 0)
            {
                var list = new List<T>(8);
                ref readonly var location = ref memory.Span[0];
                var vernier = new Vernier(memory.Length);
                while (vernier.Any())
                {
                    vernier.Flush(in location);
                    var value = converter.ToValue(memory.Slice(vernier.offset, vernier.length));
                    list.Add(value);
                }
                return list;
            }
            else
            {
                var quotient = Math.DivRem(memory.Length, definition, out var reminder);
                if (reminder != 0)
                    ThrowHelper.ThrowOverflow();
                var list = new List<T>(quotient);
                for (int i = 0; i < quotient; i++)
                    list.Add(converter.ToValue(memory.Slice(i * definition, definition)));
                return list;
            }
        }

        private readonly Converter<T> converter;
        private readonly Converter<T[]> arrayConverter;

        public ListConverter(Converter<T> converter, Converter<T[]> arrayConverter) : base(0)
        {
            var type = arrayConverter.GetType();
            this.converter = converter;
            this.arrayConverter = (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(UnmanagedArrayConverter<>)) ? arrayConverter : null;
        }

        public override void ToBytes(Allocator allocator, List<T> value) => Bytes(allocator, value, converter);

        public override List<T> ToValue(ReadOnlyMemory<byte> memory) => arrayConverter == null ? Value(memory, converter) : new List<T>(arrayConverter.ToValue(memory));
    }
}
