using Mikodev.Binary.Common;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Mikodev.Binary.Converters
{
    internal class StringConverter : ValueConverter<string>
    {
        public StringConverter() : base(0) { }

        public override void ToBytes(Allocator allocator, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            var source = Encoding.UTF8.GetBytes(value);
            var target = allocator.Allocate(source.Length);
            source.CopyTo(target);
        }

        public override string ToValue(Span<byte> block)
        {
            return block.IsEmpty ? string.Empty : MemoryMarshal.Cast<byte, char>(block).ToString();
        }
    }
}
