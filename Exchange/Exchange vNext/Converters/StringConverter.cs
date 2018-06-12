using Mikodev.Binary.Common;
using System;
using System.Runtime.InteropServices;

namespace Mikodev.Binary.Converters
{
    internal class StringConverter : ValueConverter<string>
    {
        public StringConverter() : base(0) { }

        public override void ToBytes(Allocator allocator, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            var source = MemoryMarshal.Cast<char, byte>(value.AsSpan());
            var target = allocator.Allocate(source.Length);
            source.CopyTo(target);
        }

        public override string ToValue(Span<byte> block)
        {
            return block.IsEmpty ? string.Empty : MemoryMarshal.Cast<byte, char>(block).ToString();
        }
    }
}
