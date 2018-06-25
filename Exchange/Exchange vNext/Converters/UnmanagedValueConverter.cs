using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal sealed class UnmanagedValueConverter<T> : Converter<T> where T : unmanaged
    {
        private static readonly bool origin = BitConverter.IsLittleEndian == UseLittleEndian || Unsafe.SizeOf<T>() == 1;

        internal static void BytesUnchecked(ref byte location, T value)
        {
            Unsafe.WriteUnaligned(ref location, origin ? value : Extension.ReverseEndianness(value));
        }

        internal static T ValueUnchecked(ref byte location)
        {
            return origin
                ? Unsafe.ReadUnaligned<T>(ref location)
                : Extension.ReverseEndianness(Unsafe.ReadUnaligned<T>(ref location));
        }

        internal static void Bytes(Allocator allocator, T value)
        {
            BytesUnchecked(ref allocator.Allocate(Unsafe.SizeOf<T>()).Location, value);
        }

        internal static T Value(Block block)
        {
            if (block.Length < Unsafe.SizeOf<T>())
                throw new ArgumentException();
            return ValueUnchecked(ref block.Location);
        }

        public UnmanagedValueConverter() : base(Unsafe.SizeOf<T>()) { }

        public override void ToBytes(Allocator allocator, T value) => Bytes(allocator, value);

        public override T ToValue(Block block) => Value(block);
    }
}
