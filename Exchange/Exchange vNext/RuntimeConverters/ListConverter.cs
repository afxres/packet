using Mikodev.Binary.Converters;
using System;
using System.Collections.Generic;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class ListConverter<T> : Converter<List<T>>
    {
        internal static void Bytes(Allocator allocator, List<T> value, Converter<T> converter)
        {
            if (value == null || value.Count == 0)
                return;
            if (converter.length == 0)
                for (var i = 0; i < value.Count; i++)
                    allocator.AppendValueExtend(converter, value[i]);
            else
                for (var i = 0; i < value.Count; i++)
                    converter.ToBytes(allocator, value[i]);
        }

        internal static unsafe List<T> Value(ReadOnlySpan<byte> memory, Converter<T> converter)
        {
            if (memory.IsEmpty)
                return new List<T>(0);
            var definition = converter.length;
            if (definition == 0)
            {
                var list = new List<T>(8);
                fixed (byte* srcptr = memory)
                {
                    var vernier = new Vernier(srcptr, memory.Length);
                    while (vernier.Any())
                    {
                        vernier.Update();
                        var value = converter.ToValue(memory.Slice(vernier.offset, vernier.length));
                        list.Add(value);
                    }
                }
                return list;
            }
            else
            {
                var quotient = Math.DivRem(memory.Length, definition, out var reminder);
                if (reminder != 0)
                    ThrowHelper.ThrowOverflow();
                var list = new List<T>(quotient);
                for (var i = 0; i < quotient; i++)
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

        public override List<T> ToValue(ReadOnlySpan<byte> memory) => arrayConverter == null ? Value(memory, converter) : new List<T>(arrayConverter.ToValue(memory));
    }
}
