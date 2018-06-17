using Mikodev.Binary.Common;
using System.Collections.Generic;

namespace Mikodev.Binary.CacheConverters
{
    internal sealed class ICollectionConverter<C, V> : Converter<C> where C : ICollection<V>
    {
        private readonly Converter<V> converter;

        public ICollectionConverter(Converter<V> converter) : base(0) => this.converter = converter;

        public override void ToBytes(Allocator allocator, C value)
        {
            int count;
            if (value == null || (count = value.Count) == 0)
                return;
            var array = new V[count];
            value.CopyTo(array, 0);
            ArrayConverter<V>.ToBytes(allocator, array, converter);
        }

        public override C ToValue(Block block)
        {
            throw new System.NotImplementedException();
        }
    }
}
