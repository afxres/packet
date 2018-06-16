using Mikodev.Binary.Common;
using System;

namespace Mikodev.Binary.CacheConverters
{
    internal sealed class ByPropertiesConverter<T> : Converter<T>
    {
        private readonly Action<Allocator, T> toBytesAction;

        public ByPropertiesConverter(Action<Allocator, T> toBytesAction) : base(0)
        {
            this.toBytesAction = toBytesAction;
        }

        public override void ToBytes(Allocator allocator, T value) => toBytesAction.Invoke(allocator, value);

        public override T ToValue(Block block)
        {
            throw new NotImplementedException();
        }
    }
}
