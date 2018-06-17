using Mikodev.Binary.Common;
using System.Collections.Generic;

namespace Mikodev.Binary.CacheConverters
{
    internal sealed class ICollectionConverter<T> : Converter<ICollection<T>>
    {
        private readonly Converter<T> converter;

        public ICollectionConverter(Converter<T> converter) : base(0) => this.converter = converter;

        public override void ToBytes(Allocator allocator, ICollection<T> value)
        {
            int count;
            if (value == null || (count = value.Count) == 0)
                return;
            var array = new T[count];
            value.CopyTo(array, 0);
            ArrayConverter<T>.ToBytes(allocator, array, converter);
        }

        public override ICollection<T> ToValue(Block block)
        {
            throw new System.NotImplementedException();
        }
    }
}
