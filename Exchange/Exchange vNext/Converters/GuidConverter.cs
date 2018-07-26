using System;
using uint16 = System.UInt16;
using uint32 = System.UInt32;
using uint64 = System.UInt64;

namespace Mikodev.Binary.Converters
{
    internal sealed class GuidConverter : Converter<Guid>
    {
        private static readonly bool origin = BitConverter.IsLittleEndian == UseLittleEndian;

        public unsafe GuidConverter() : base(sizeof(Guid)) { }

        private static unsafe void Swap(byte* target, byte* source)
        {
            *(uint32*)(target + 0) = Endian.Swap32(*(uint32*)(source + 0));
            *(uint16*)(target + 4) = Endian.Swap16(*(uint16*)(source + 4));
            *(uint16*)(target + 6) = Endian.Swap16(*(uint16*)(source + 6));
            *(uint64*)(target + 8) = *(uint64*)(source + 8);
        }

        public override unsafe void ToBytes(Allocator allocator, Guid value)
        {
            var memory = allocator.Allocate(sizeof(Guid));
            fixed (byte* target = &memory.Span[0])
            {
                if (origin)
                    *(Guid*)target = value;
                else
                    Swap(target, (byte*)&value);
            }
        }

        public override unsafe Guid ToValue(ReadOnlyMemory<byte> memory)
        {
            if (memory.Length < sizeof(Guid))
                ThrowHelper.ThrowOverflow();
            var result = default(Guid);
            fixed (byte* source = &memory.Span[0])
            {
                if (origin)
                    result = *(Guid*)source;
                else
                    Swap((byte*)&result, source);
            }
            return result;
        }
    }
}
