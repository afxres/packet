using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(TimeSpan))]
    internal sealed class TimeSpanConverter : PacketConverter<TimeSpan>
    {
        public static byte[] ToBytes(TimeSpan value) => BitConverter.GetBytes(value.Ticks);

        public static TimeSpan ToValue(byte[] buffer, int offset) => new TimeSpan(BitConverter.ToInt64(buffer, offset));

        public override int Length => sizeof(Int64);

        public override byte[] GetBytes(TimeSpan value) => ToBytes(value);

        public override TimeSpan GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset);

        public override byte[] GetBytes(object value) => ToBytes((TimeSpan)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset);
    }
}
