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
                    var stream = allocator.stream;
                    for (int i = 0; i < value.Count; i++)
                    {
                        offset = stream.AnchorExtend();
                        converter.ToBytes(allocator, value[i]);
                        stream.FinishExtend(offset);
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

        internal static List<T> Value(Block block, Converter<T> converter)
        {
            if (block.IsEmpty)
                return new List<T>(0);
            if (converter.Length == 0)
            {
                var list = new List<T>(8);
                var vernier = (Vernier)block;
                while (vernier.Any)
                {
                    vernier.Flush();
                    list.Add(converter.ToValue((Block)vernier));
                }
                return list;
            }
            else
            {
                var quotient = Math.DivRem(block.Length, converter.Length, out var reminder);
                if (reminder != 0)
                    ThrowHelper.ThrowOverflow();
                var list = new List<T>(quotient);
                for (int i = 0; i < quotient; i++)
                    list.Add(converter.ToValue(new Block(block.Buffer, block.Offset + i * converter.Length, converter.Length)));
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

        public override List<T> ToValue(Block block) => arrayConverter == null ? Value(block, converter) : new List<T>(arrayConverter.ToValue(block));
    }
}
