using Mikodev.Binary.Common;
using System;

namespace Mikodev.Binary.CacheConverters
{
    internal sealed class ByPropertiesConverter<T> : Converter<T>
    {
        private readonly Action<Allocator, T> toBytes;

        public ByPropertiesConverter(Action<Allocator, T> toBytes) : base(0)
        {
            this.toBytes = toBytes;
        }

        public override void ToBytes(Allocator allocator, T value) => toBytes.Invoke(allocator, value);

        public override T ToValue(Block block)
        {
            throw new NotImplementedException();
        }
    }
}
