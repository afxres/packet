using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal sealed class UnmanagedValueConverter<T> : Converter<T> where T : unmanaged
    {
        private static readonly bool origin = BitConverter.IsLittleEndian == UseLittleEndian || Unsafe.SizeOf<T>() == 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ToBytesUnchecked(ref byte location, T value)
        {
            Unsafe.WriteUnaligned(ref location, origin ? value : Endian.Reverse(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T ToValueUnchecked(ref byte location)
        {
            var value = Unsafe.ReadUnaligned<T>(ref location);
            return origin ? value : Endian.Reverse(value);
        }

        internal static void Bytes(Allocator allocator, T value)
        {
            ToBytesUnchecked(ref allocator.Allocate(Unsafe.SizeOf<T>()).Location, value);
        }

        internal static T Value(Block block)
        {
            if (block.Length < Unsafe.SizeOf<T>())
                ThrowHelper.ThrowOverflow();
            return ToValueUnchecked(ref block.Location);
        }

        public UnmanagedValueConverter() : base(Unsafe.SizeOf<T>()) { }

        public override void ToBytes(Allocator allocator, T value) => Bytes(allocator, value);

        public override T ToValue(Block block) => Value(block);
    }
}
