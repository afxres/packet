using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(DateTime))]
    internal sealed class DateTimeConverter : PacketConverter<DateTime>
    {
        public static byte[] ToBytes(DateTime value) => BitConverter.GetBytes(value.ToBinary());

        public static DateTime ToValue(byte[] buffer, int offset) => DateTime.FromBinary(BitConverter.ToInt64(buffer, offset));

        public override int Length => sizeof(Int64);

        public override byte[] GetBytes(DateTime value) => ToBytes(value);

        public override DateTime GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset);

        public override byte[] GetBuffer(object value) => ToBytes((DateTime)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset);
    }
}
