using System;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class ObjectConverter : Converter<object>
    {
        private readonly Cache cache;

        public ObjectConverter(Cache cache) : base(0) => this.cache = cache;

        public override void ToBytes(Allocator allocator, object value)
        {
            if (value == null)
                return;
            var type = value.GetType();
            if (type == typeof(object))
                throw new InvalidOperationException("Invalid type : object");
            var converter = cache.GetConverter(type);
            converter.ToBytesAny(allocator, value);
        }

        public override object ToValue(Memory<byte> memory) => new Token(cache, memory);
    }
}
