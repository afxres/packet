using Mikodev.Binary.Common;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal class UnmanagedValueConverter<T> : ValueConverter<T> where T : unmanaged
    {
        public UnmanagedValueConverter() : base(Unsafe.SizeOf<T>()) { }

        public override void ToBytes(Allocator allocator, T value) => Unsafe.WriteUnaligned(ref allocator.Allocate(Unsafe.SizeOf<T>()).Location, value);

        public override T ToValue(Allocation allocation) => Unsafe.ReadUnaligned<T>(ref allocation.Location);
    }
}
