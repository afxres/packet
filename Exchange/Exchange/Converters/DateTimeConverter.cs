using System;

namespace Mikodev.Network.Converters
{
    [Converter(typeof(DateTime))]
    internal sealed class DateTimeConverter : PacketConverter<DateTime>
    {
        private static byte[] ToBytes(DateTime value) => UnmanagedConverter<long>.ToBytes(value.ToBinary());

        private static DateTime ToValue(byte[] buffer, int offset, int length) => DateTime.FromBinary(UnmanagedConverter<long>.ToValue(buffer, offset, length));

        public override int Length => sizeof(Int64);

        public override byte[] GetBytes(DateTime value) => ToBytes(value);

        public override DateTime GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override byte[] GetBytes(object value) => ToBytes((DateTime)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
