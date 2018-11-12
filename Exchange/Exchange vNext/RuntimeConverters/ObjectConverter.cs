using System;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class ObjectConverter : Converter<object>
    {
        private Cache cache;

        public ObjectConverter() : base(0) { }

        protected override void OnInitialize(Cache cache)
        {
            this.cache = cache;
        }

        public override void ToBytes(ref Allocator allocator, object value)
        {
            EnsureInitialized();

            if (value == null)
                return;
            var type = value.GetType();
            if (type == typeof(object))
                throw new InvalidOperationException($"Invalid type: {typeof(object)}");
            var converter = cache.GetConverter(type);
            converter.ToBytesAny(ref allocator, value);
        }

        public override object ToValue(ReadOnlySpan<byte> memory) => throw new InvalidOperationException($"Invalid type: {typeof(object)}");
    }
}
