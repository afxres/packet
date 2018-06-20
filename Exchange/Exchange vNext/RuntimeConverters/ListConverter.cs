using Mikodev.Binary.Common;
using System;
using System.Collections.Generic;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class ListConverter<T> : Converter<List<T>>
    {
        internal static void ToBytes(Allocator allocator, List<T> value, Converter<T> converter)
        {
            if (value != null && value.Count != 0)
            {
                if (converter.Length == 0)
                {
                    int offset;
                    var stream = allocator.stream;
                    for (int i = 0; i < value.Count; i++)
                    {
                        offset = stream.BeginModify();
                        converter.ToBytes(allocator, value[i]);
                        stream.EndModify(offset);
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

        internal static List<T> ToValue(Block block, Converter<T> converter)
        {
            if (block.IsEmpty)
                return new List<T>();
            if (converter.Length == 0)
            {
                var list = new List<T>();
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
                    throw new OverflowException();
                var list = new List<T>(quotient);
                for (int i = 0; i < quotient; i++)
                    list.Add(converter.ToValue(new Block(block.Buffer, block.Offset + i * converter.Length, converter.Length)));
                return list;
            }
        }

        private readonly Converter<T> converter;

        public ListConverter(Converter<T> converter) : base(0) => this.converter = converter;

        public override void ToBytes(Allocator allocator, List<T> value) => ToBytes(allocator, value, converter);

        public override List<T> ToValue(Block block) => ToValue(block, converter);
    }
}
