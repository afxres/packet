using Mikodev.Binary.Common;
using System.Collections.Generic;

namespace Mikodev.Binary.CacheConverters
{
    internal sealed class IEnumerableConverter<T> : Converter<IEnumerable<T>>
    {
        private readonly Converter<T> converter;

        public IEnumerableConverter(Converter<T> converter) : base(0) => this.converter = converter;

        public override void ToBytes(Allocator allocator, IEnumerable<T> value)
        {
            if (value != null)
            {
                if (converter.Length == 0)
                {
                    var stream = allocator.stream;
                    foreach (var i in value)
                    {
                        var source = stream.BeginModify();
                        converter.ToBytes(allocator, i);
                        stream.EndModify(source);
                    }
                }
                else
                {
                    foreach (var i in value)
                    {
                        converter.ToBytes(allocator, i);
                    }
                }
            }
        }

        public override IEnumerable<T> ToValue(Block block)
        {
            throw new System.NotImplementedException();
        }
    }
}
