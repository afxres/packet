using Mikodev.Binary.Common;
using System;

namespace Mikodev.Binary.CacheConverters
{
    internal sealed class ArrayConverter<T> : Converter<T[]>
    {
        internal static void ToBytes(Allocator allocator, T[] value, Converter<T> converter)
        {
            if (value != null && value.Length != 0)
            {
                if (converter.Length == 0)
                {
                    var stream = allocator.stream;
                    for (int i = 0; i < value.Length; i++)
                    {
                        var source = stream.BeginModify();
                        converter.ToBytes(allocator, value[i]);
                        stream.EndModify(source);
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
        }

        private readonly Converter<T> converter;

        public ArrayConverter(Converter<T> converter) : base(0) => this.converter = converter;

        public override void ToBytes(Allocator allocator, T[] value) => ToBytes(allocator, value, converter);

        public override T[] ToValue(Block block)
        {
            throw new NotImplementedException();
        }
    }
}
