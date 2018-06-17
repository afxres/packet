using Mikodev.Binary.Common;
using System.Collections.Generic;

namespace Mikodev.Binary.CacheConverters
{
    internal sealed class IEnumerableConverter<E, V> : Converter<E> where E : IEnumerable<V>
    {
        private readonly Converter<V> converter;

        public IEnumerableConverter(Converter<V> converter) : base(0) => this.converter = converter;

        public override void ToBytes(Allocator allocator, E value)
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

        public override E ToValue(Block block)
        {
            throw new System.NotImplementedException();
        }
    }
}
