using System;
using uint16 = System.UInt16;
using uint32 = System.UInt32;
using uint64 = System.UInt64;

namespace Mikodev.Network.Converters
{
    [Converter(typeof(Guid))]
    internal sealed class GuidConverter : PacketConverter<Guid>
    {
        private const int SizeOf = 16;

        private static readonly bool origin = BitConverter.IsLittleEndian == PacketConvert.UseLittleEndian;

        private static unsafe void Swap(byte* target, byte* source)
        {
            *(uint32*)(target + 0) = Endian.Swap32(*(uint32*)(source + 0));
            *(uint16*)(target + 4) = Endian.Swap16(*(uint16*)(source + 4));
            *(uint16*)(target + 6) = Endian.Swap16(*(uint16*)(source + 6));
            *(uint64*)(target + 8) = *(uint64*)(source + 8);
        }

        private static unsafe byte[] ToBytes(Guid value)
        {
            var result = new byte[SizeOf];
            ref var target = ref result[0];
            fixed (byte* pointer = &target)
            {
                if (origin)
                    *(Guid*)pointer = value;
                else
                    Swap(pointer, (byte*)&value);
            }
            return result;
        }

        private static unsafe Guid ToValue(byte[] buffer, int offset, int length)
        {
            if (buffer == null || offset < 0 || length < SizeOf || buffer.Length - offset < length)
                throw PacketException.Overflow();
            var result = default(Guid);
            fixed (byte* source = &buffer[offset])
            {
                if (origin)
                    result = *(Guid*)source;
                else
                    Swap((byte*)&result, source);
            }
            return result;
        }

        public GuidConverter() : base(SizeOf) { }

        public override byte[] GetBytes(Guid value) => ToBytes(value);

        public override Guid GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override byte[] GetBytes(object value) => ToBytes((Guid)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
