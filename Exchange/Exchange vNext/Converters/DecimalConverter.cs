using System;

namespace Mikodev.Binary.Converters
{
    internal sealed class DecimalConverter : Converter<decimal>
    {
        private static readonly bool origin = BitConverter.IsLittleEndian == UseLittleEndian;

        public DecimalConverter() : base(sizeof(decimal)) { }

        public override void ToBytes(Allocator allocator, decimal value)
        {
            var bits = decimal.GetBits(value);
            var memory = allocator.Allocate(sizeof(decimal));
            var span = memory.Span;
            if (origin)
                Unsafe.Copy(ref span[0], in bits[0], sizeof(decimal));
            else
                Endian.SwapCopy(ref span[0], in bits[0], sizeof(decimal));
        }

        public override decimal ToValue(ReadOnlyMemory<byte> memory)
        {
            if (memory.Length < sizeof(decimal))
                ThrowHelper.ThrowOverflow();
            var bits = new int[4];
            var span = memory.Span;
            if (origin)
                Unsafe.Copy(ref bits[0], in span[0], sizeof(decimal));
            else
                Endian.SwapCopy(ref bits[0], in span[0], sizeof(decimal));
            return new decimal(bits);
        }
    }
}
