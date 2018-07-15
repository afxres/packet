using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal sealed class UnmanagedValueConverter<T> : Converter<T> where T : unmanaged
    {
        private static readonly bool origin = BitConverter.IsLittleEndian == UseLittleEndian || Unsafe.SizeOf<T>() == 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void UnsafeToBytes(ref byte location, T value)
        {
            if (origin)
                Unsafe.WriteUnaligned(ref location, value);
            else
                Endian.Swap<T>(ref location, ref Unsafe.As<T, byte>(ref value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T UnsafeToValue(ref byte location)
        {
            if (origin)
                return Unsafe.ReadUnaligned<T>(ref location);
            var result = default(T);
            Endian.Swap<T>(ref Unsafe.As<T, byte>(ref result), ref location);
            return result;
        }

        internal static void SafeToBytes(Allocator allocator, T value)
        {
            UnsafeToBytes(ref allocator.Allocate(Unsafe.SizeOf<T>())[0], value);
        }

        internal static T SafeToValue(Block block)
        {
            if (block.Length < Unsafe.SizeOf<T>())
                ThrowHelper.ThrowOverflow();
            return UnsafeToValue(ref block[0]);
        }

        public UnmanagedValueConverter() : base(Unsafe.SizeOf<T>()) { }

        public override void ToBytes(Allocator allocator, T value) => SafeToBytes(allocator, value);

        public override T ToValue(Block block) => SafeToValue(block);
    }
}
