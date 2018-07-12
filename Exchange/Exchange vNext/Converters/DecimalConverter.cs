using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal sealed class DecimalConverter : Converter<decimal>
    {
        private static readonly bool reverse = BitConverter.IsLittleEndian != UseLittleEndian;

        public DecimalConverter() : base(sizeof(decimal)) { }

        public override void ToBytes(Allocator allocator, decimal value)
        {
            var bits = decimal.GetBits(value);
            var block = allocator.Allocate(sizeof(decimal));
            if (reverse)
                Endian.ReverseRange(ref bits[0], sizeof(decimal));
            Unsafe.CopyBlockUnaligned(ref block[0], ref Unsafe.As<int, byte>(ref bits[0]), sizeof(decimal));
        }

        public override decimal ToValue(Block block)
        {
            if (block.Length < sizeof(decimal))
                ThrowHelper.ThrowOverflow();
            var bits = new int[4];
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<int, byte>(ref bits[0]), ref block[0], sizeof(decimal));
            if (reverse)
                Endian.ReverseRange(ref bits[0], sizeof(decimal));
            return new decimal(bits);
        }
    }
}
