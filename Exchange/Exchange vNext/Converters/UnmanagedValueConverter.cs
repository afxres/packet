using Mikodev.Binary.Common;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal class UnmanagedValueConverter<T> : Converter<T> where T : unmanaged
    {
        public UnmanagedValueConverter() : base(Unsafe.SizeOf<T>()) { }

        public override void ToBytes(Allocator allocator, T value) => Unsafe.WriteUnaligned(ref allocator.Allocate(Unsafe.SizeOf<T>()).Location, value);

        public override T ToValue(Block block) => Unsafe.ReadUnaligned<T>(ref block.Location);
    }
}
