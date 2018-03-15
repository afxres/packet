using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(DateTime))]
    internal sealed class DateTimeConverter : IPacketConverter, IPacketConverter<DateTime>
    {
        public static byte[] ToBytes(DateTime value) => BitConverter.GetBytes(value.ToBinary());

        public static DateTime ToDateTime(byte[] buffer, int offset) => DateTime.FromBinary(BitConverter.ToInt64(buffer, offset));

        public int Length => sizeof(Int64);

        public byte[] GetBytes(DateTime value) => ToBytes(value);

        public DateTime GetValue(byte[] buffer, int offset, int length) => ToDateTime(buffer, offset);

        byte[] IPacketConverter.GetBytes(object value) => ToBytes((DateTime)value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => ToDateTime(buffer, offset);
    }
}
