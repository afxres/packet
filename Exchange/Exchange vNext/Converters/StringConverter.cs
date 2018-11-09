using System;

namespace Mikodev.Binary.Converters
{
    internal sealed class StringConverter : Converter<string>
    {
        public StringConverter() : base(0) { }

        public override void ToBytes(ref Allocator allocator, string value)
        {
            allocator.Append(value.AsSpan());
        }

        public override unsafe string ToValue(ReadOnlySpan<byte> memory)
        {
            if (memory.IsEmpty)
                return string.Empty;
            fixed (byte* srcptr = memory)
                return Encoding.GetString(srcptr, memory.Length);
        }
    }
}
