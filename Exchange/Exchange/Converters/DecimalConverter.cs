using System;

namespace Mikodev.Network.Converters
{
    [_Converter(typeof(Decimal))]
    internal class DecimalConverter : IPacketConverter, IPacketConverter<Decimal>
    {
        public static byte[] ToBytes(Decimal value)
        {
            var arr = Decimal.GetBits(value);
            var buf = new byte[sizeof(Decimal)];
            for (int i = 0; i < arr.Length; i++)
                Buffer.BlockCopy(BitConverter.GetBytes(arr[i]), 0, buf, i * sizeof(int), sizeof(int));
            return buf;
        }

        public static Decimal ToDecimal(byte[] buffer, int offset)
        {
            var arr = new int[sizeof(Decimal) / sizeof(int)];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = BitConverter.ToInt32(buffer, offset + i * sizeof(int));
            var val = new Decimal(arr);
            return val;
        }

        public int Length => sizeof(Decimal);

        public byte[] GetBytes(decimal value) => ToBytes(value);

        public decimal GetValue(byte[] buffer, int offset, int length) => ToDecimal(buffer, offset);

        byte[] IPacketConverter.GetBytes(object value) => ToBytes((Decimal)value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => ToDecimal(buffer, offset);
    }
}
