using System;

namespace Mikodev.Binary.Converters
{
    internal sealed class StringConverter : Converter<string>
    {
        public StringConverter() : base(0) { }

        public override void ToBytes(Allocator allocator, string value)
        {
            allocator.Append(value.AsSpan());
        }

        public override string ToValue(ReadOnlyMemory<byte> memory)
        {
            if (memory.IsEmpty)
                return string.Empty;
            var span = memory.Span;
            return Encoding.GetString(in span[0], span.Length);
        }
    }
}
