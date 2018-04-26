using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(TimeSpan))]
    internal sealed class TimeSpanConverter : IPacketConverter, IPacketConverter<TimeSpan>
    {
        public static byte[] ToBytes(TimeSpan value) => BitConverter.GetBytes(value.Ticks);

        public static TimeSpan ToValue(byte[] buffer, int offset) => new TimeSpan(BitConverter.ToInt64(buffer, offset));

        public int Length => sizeof(Int64);

        public byte[] GetBytes(TimeSpan value) => ToBytes(value);

        public TimeSpan GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset);

        byte[] IPacketConverter.GetBytes(object value) => ToBytes((TimeSpan)value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset);
    }
}
