using System;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class ObjectConverter : Converter<object>
    {
        public ObjectConverter() : base(0) { }

        public override void ToBytes(Allocator allocator, object value)
        {
            if (value == null)
                return;
            var type = value.GetType();
            if (type == typeof(object))
                throw new InvalidOperationException("Invalid type: object");
            var converter = GetConverter(type);
            converter.ToBytesAny(allocator, value);
        }

        public override object ToValue(ReadOnlyMemory<byte> memory) => new Token(cache, memory);
    }
}
