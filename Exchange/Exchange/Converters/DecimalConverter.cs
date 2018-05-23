using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(Decimal))]
    internal sealed class DecimalConverter : PacketConverter<Decimal>
    {
        public static byte[] ToBytes(Decimal value)
        {
            var arr = Decimal.GetBits(value);
            var buf = new byte[sizeof(Decimal)];
            for (int i = 0; i < arr.Length; i++)
                Buffer.BlockCopy(BitConverter.GetBytes(arr[i]), 0, buf, i * sizeof(int), sizeof(int));
            return buf;
        }

        public static Decimal ToValue(byte[] buffer, int offset)
        {
            var arr = new int[sizeof(Decimal) / sizeof(int)];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = BitConverter.ToInt32(buffer, offset + i * sizeof(int));
            var val = new Decimal(arr);
            return val;
        }

        public override int Length => sizeof(Decimal);

        public override byte[] GetBytes(decimal value) => ToBytes(value);

        public override decimal GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset);

        public override byte[] GetBuffer(object value) => ToBytes((Decimal)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset);
    }
}
