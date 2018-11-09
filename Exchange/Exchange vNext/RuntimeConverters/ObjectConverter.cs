using System;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class ObjectConverter : Converter<object>
    {
        public ObjectConverter() : base(0) { }

        public override void ToBytes(ref Allocator allocator, object value)
        {
            if (value == null)
                return;
            var type = value.GetType();
            if (type == typeof(object))
                throw new InvalidOperationException($"Invalid type: {typeof(object)}");
            var converter = GetConverter(type);
            converter.ToBytesAny(ref allocator, value);
        }

        public override object ToValue(ReadOnlySpan<byte> memory) => throw new InvalidOperationException($"Invalid type: {typeof(object)}");
    }
}
