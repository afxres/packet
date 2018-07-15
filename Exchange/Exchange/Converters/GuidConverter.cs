using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Network.Converters
{
    [Converter(typeof(Guid))]
    internal sealed class GuidConverter : PacketConverter<Guid>
    {
        private const int SizeOf = 16;

        private static readonly bool origin = BitConverter.IsLittleEndian == PacketConvert.UseLittleEndian;

        private static byte[] ToBytes(Guid value)
        {
            var result = new byte[SizeOf];
            ref var target = ref result[0];
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
            return result;
        }

        private static Guid ToValue(byte[] buffer, int offset, int length)
        {
            if (buffer == null || offset < 0 || length < SizeOf || buffer.Length - offset < length)
                throw PacketException.Overflow();
            ref var source = ref buffer[offset];
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

        public GuidConverter() : base(SizeOf) { }

        public override byte[] GetBytes(Guid value) => ToBytes(value);

        public override Guid GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override byte[] GetBytes(object value) => ToBytes((Guid)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
