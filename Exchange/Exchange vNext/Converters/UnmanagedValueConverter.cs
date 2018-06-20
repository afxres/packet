using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal sealed class UnmanagedValueConverter<T> : Converter<T> where T : unmanaged
    {
        internal static T ToValueUnchecked(ref byte location) => Unsafe.ReadUnaligned<T>(ref location);

        public UnmanagedValueConverter() : base(Unsafe.SizeOf<T>()) { }

        public override void ToBytes(Allocator allocator, T value) => Unsafe.WriteUnaligned(ref allocator.Allocate(Unsafe.SizeOf<T>()).Location, value);

        public override T ToValue(Block block) => ToValueUnchecked(ref block.Location);
    }
}
