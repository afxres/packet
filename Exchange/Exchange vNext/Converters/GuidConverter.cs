using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal sealed class GuidConverter : Converter<Guid>
    {
        private const int X1 = sizeof(uint);
        private const int X2 = X1 + sizeof(ushort);
        private const int X3 = X2 + sizeof(ushort);
        private const int L4 = 8;
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
                Unsafe.WriteUnaligned(ref buffer[offset], Extension.ReverseEndianness(Unsafe.As<byte, uint>(ref source)));
                Unsafe.WriteUnaligned(ref buffer[offset + X1], Extension.ReverseEndianness(Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref source, (IntPtr)X1))));
                Unsafe.WriteUnaligned(ref buffer[offset + X2], Extension.ReverseEndianness(Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref source, (IntPtr)X2))));
                Unsafe.CopyBlockUnaligned(ref buffer[offset + X3], ref Unsafe.AddByteOffset(ref source, (IntPtr)X3), L4);
            }
        }

        public override Guid ToValue(Block block)
        {
            if (block.Length < Unsafe.SizeOf<Guid>())
                throw new ArgumentException();
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
                Unsafe.WriteUnaligned(ref target, Extension.ReverseEndianness(Unsafe.As<byte, uint>(ref buffer[offset])));
                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref target, (IntPtr)X1), Extension.ReverseEndianness(Unsafe.As<byte, ushort>(ref buffer[offset + X1])));
                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref target, (IntPtr)X2), Extension.ReverseEndianness(Unsafe.As<byte, ushort>(ref buffer[offset + X2])));
                Unsafe.CopyBlockUnaligned(ref Unsafe.AddByteOffset(ref target, (IntPtr)X3), ref buffer[offset + X3], L4);
                return value;
            }
        }
    }
}
