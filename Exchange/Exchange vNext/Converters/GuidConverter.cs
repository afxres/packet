using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal sealed class GuidConverter : Converter<Guid>
    {
        private static readonly bool origin = BitConverter.IsLittleEndian == UseLittleEndian;

        public GuidConverter() : base(Unsafe.SizeOf<Guid>()) { }

        public override void ToBytes(Allocator allocator, Guid value)
        {
            var memory = allocator.Allocate(Unsafe.SizeOf<Guid>());
            var span = memory.Span;
            ref var target = ref span[0];
            if (origin)
            {
                Unsafe.WriteUnaligned(ref target, value);
            }
            else
            {
                ref var source = ref Unsafe.As<Guid, byte>(ref value);
                Unsafe.WriteUnaligned(ref target, Endian.Swap32(Unsafe.As<byte, uint>(ref source)));
                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref target, (IntPtr)4), Endian.Swap16(Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref source, (IntPtr)4))));
                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref target, (IntPtr)6), Endian.Swap16(Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref source, (IntPtr)6))));
                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref target, (IntPtr)8), Unsafe.As<byte, ulong>(ref Unsafe.AddByteOffset(ref source, (IntPtr)8)));
            }
        }

        public override Guid ToValue(Memory<byte> memory)
        {
            if (memory.Length < Unsafe.SizeOf<Guid>())
                ThrowHelper.ThrowOverflow();
            var span = memory.Span;
            ref var source = ref span[0];
            if (origin)
            {
                return Unsafe.ReadUnaligned<Guid>(ref source);
            }
            else
            {
                var value = default(Guid);
                ref var target = ref Unsafe.As<Guid, byte>(ref value);
                Unsafe.WriteUnaligned(ref target, Endian.Swap32(Unsafe.As<byte, uint>(ref source)));
                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref target, (IntPtr)4), Endian.Swap16(Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref source, (IntPtr)4))));
                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref target, (IntPtr)6), Endian.Swap16(Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref source, (IntPtr)6))));
                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref target, (IntPtr)8), Unsafe.As<byte, ulong>(ref Unsafe.AddByteOffset(ref source, (IntPtr)8)));
                return value;
            }
        }
    }
}
