using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal sealed class UnmanagedValueConverter<T> : Converter<T> where T : unmanaged
    {
        private static readonly bool origin = BitConverter.IsLittleEndian == UseLittleEndian || Unsafe.SizeOf<T>() == 1;

        internal static void ToBytesUnchecked(ref byte location, T value)
        {
            Unsafe.WriteUnaligned(ref location, origin ? value : Extension.ReverseEndianness(value));
        }

        internal static T ToValueUnchecked(ref byte location)
        {
            return origin
                ? Unsafe.ReadUnaligned<T>(ref location)
                : Extension.ReverseEndianness(Unsafe.ReadUnaligned<T>(ref location));
        }

        internal static void ToBytesNormal(Allocator allocator, T value)
        {
            ToBytesUnchecked(ref allocator.Allocate(Unsafe.SizeOf<T>()).Location, value);
        }

        internal static T ToValueNormal(Block block)
        {
            if (block.Length < Unsafe.SizeOf<T>())
                throw new ArgumentException();
            return ToValueUnchecked(ref block.Location);
        }

        public UnmanagedValueConverter() : base(Unsafe.SizeOf<T>()) { }

        public override void ToBytes(Allocator allocator, T value) => ToBytesNormal(allocator, value);

        public override T ToValue(Block block) => ToValueNormal(block);
    }
}
