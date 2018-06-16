using Mikodev.Binary.Common;
using System.Runtime.CompilerServices;
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
            var block = allocator.Allocate(source.Length);
            Unsafe.CopyBlockUnaligned(ref block.Location, ref source[0], (uint)block.Length);
        }

        public override string ToValue(Block block)
        {
            return block.IsEmpty ? string.Empty : Encoding.UTF8.GetString(block.Buffer, block.Offset, block.Length);
        }
    }
}
