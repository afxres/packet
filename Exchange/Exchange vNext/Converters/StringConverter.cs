using Mikodev.Binary.Common;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal sealed class StringConverter : Converter<string>
    {
        public StringConverter() : base(0) { }

        public override void ToBytes(Allocator allocator, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            var source = Extension.Encoding.GetBytes(value);
            var block = allocator.Allocate(source.Length);
            Unsafe.CopyBlockUnaligned(ref block.Location, ref source[0], (uint)block.Length);
        }

        public override string ToValue(Block block)
        {
            return block.IsEmpty ? string.Empty : Extension.Encoding.GetString(block.Buffer, block.Offset, block.Length);
        }
    }
}
