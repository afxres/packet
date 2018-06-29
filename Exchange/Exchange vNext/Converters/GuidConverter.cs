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
            var block = allocator.Allocate(Unsafe.SizeOf<Guid>());
            if (origin)
            {
                Unsafe.WriteUnaligned(ref block.Location, value);
            }
            else
            {
                var buffer = block.Buffer;
                var offset = block.Offset;
                ref var source = ref Unsafe.As<Guid, byte>(ref value);
                Unsafe.WriteUnaligned(ref buffer[offset], Endian.Reverse32(Unsafe.As<byte, uint>(ref source)));
                Unsafe.WriteUnaligned(ref buffer[offset + 4], Endian.Reverse16(Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref source, (IntPtr)4))));
                Unsafe.WriteUnaligned(ref buffer[offset + 6], Endian.Reverse16(Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref source, (IntPtr)6))));
                Unsafe.WriteUnaligned(ref buffer[offset + 8], Unsafe.As<byte, ulong>(ref Unsafe.AddByteOffset(ref source, (IntPtr)8)));
            }
        }

        public override Guid ToValue(Block block)
        {
            if (block.Length < Unsafe.SizeOf<Guid>())
                ThrowHelper.ThrowOverflow();
            if (origin)
            {
                return Unsafe.ReadUnaligned<Guid>(ref block.Location);
            }
            else
            {
                var buffer = block.Buffer;
                var offset = block.Offset;
                var value = default(Guid);
                ref var target = ref Unsafe.As<Guid, byte>(ref value);
                Unsafe.WriteUnaligned(ref target, Endian.Reverse32(Unsafe.As<byte, uint>(ref buffer[offset])));
                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref target, (IntPtr)4), Endian.Reverse16(Unsafe.As<byte, ushort>(ref buffer[offset + 4])));
                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref target, (IntPtr)6), Endian.Reverse16(Unsafe.As<byte, ushort>(ref buffer[offset + 6])));
                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref target, (IntPtr)8), Unsafe.As<byte, ulong>(ref buffer[offset + 8]));
                return value;
            }
        }
    }
}
