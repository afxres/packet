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
            var allocation = allocator.Allocate(source.Length);
            Unsafe.CopyBlockUnaligned(ref allocation.Location, ref source[0], (uint)allocation.Length);
        }

        public override string ToValue(Allocation allocation)
        {
            return allocation.IsEmpty ? string.Empty : Encoding.UTF8.GetString(allocation.Buffer, allocation.Offset, allocation.Length);
        }
    }
}
