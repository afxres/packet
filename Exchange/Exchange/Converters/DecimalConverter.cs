using System.Runtime.CompilerServices;

namespace Mikodev.Network.Converters
{
    [Converter(typeof(decimal))]
    internal sealed class DecimalConverter : PacketConverter<decimal>
    {
        private static byte[] ToBytes(decimal value)
        {
            var source = decimal.GetBits(value);
            var target = new byte[sizeof(decimal)];
            Unsafe.CopyBlockUnaligned(ref target[0], ref Unsafe.As<int, byte>(ref source[0]), sizeof(decimal));
            return target;
        }

        private static decimal ToValue(byte[] buffer, int offset, int length)
        {
            if (buffer == null || offset < 0 || length < sizeof(decimal) || buffer.Length - offset < length)
                throw PacketException.Overflow();
            var target = new int[sizeof(decimal) / sizeof(int)];
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<int, byte>(ref target[0]), ref buffer[offset], sizeof(decimal));
            var result = new decimal(target);
            return result;
        }

        public override int Length => sizeof(decimal);

        public override byte[] GetBytes(decimal value) => ToBytes(value);

        public override decimal GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override byte[] GetBytes(object value) => ToBytes((decimal)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
