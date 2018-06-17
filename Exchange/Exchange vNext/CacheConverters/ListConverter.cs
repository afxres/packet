using Mikodev.Binary.Common;
using System;
using System.Collections.Generic;

namespace Mikodev.Binary.CacheConverters
{
    internal sealed class ListConverter<T> : Converter<List<T>>
    {
        internal static void ToBytes(Allocator allocator, List<T> value, Converter<T> converter)
        {
            if (value != null && value.Count != 0)
            {
                if (converter.Length == 0)
                {
                    var stream = allocator.stream;
                    for (int i = 0; i < value.Count; i++)
                    {
                        var source = stream.BeginModify();
                        converter.ToBytes(allocator, value[i]);
                        stream.EndModify(source);
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

        private readonly Converter<T> converter;

        public ListConverter(Converter<T> converter) : base(0) => this.converter = converter;

        public override void ToBytes(Allocator allocator, List<T> value) => ToBytes(allocator, value, converter);

        public override List<T> ToValue(Block block)
        {
            throw new NotImplementedException();
        }
    }
}
