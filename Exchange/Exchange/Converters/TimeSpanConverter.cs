using System;

namespace Mikodev.Network.Converters
{
    [Converter(typeof(TimeSpan))]
    internal sealed class TimeSpanConverter : PacketConverter<TimeSpan>
    {
        private static byte[] ToBytes(TimeSpan value) => UnmanagedValueConverter<long>.ToBytes(value.Ticks);

        private static TimeSpan ToValue(byte[] buffer, int offset, int length) => new TimeSpan(UnmanagedValueConverter<long>.ToValue(buffer, offset, length));

        public override int Length => sizeof(Int64);

        public override byte[] GetBytes(TimeSpan value) => ToBytes(value);

        public override TimeSpan GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override byte[] GetBytes(object value) => ToBytes((TimeSpan)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
