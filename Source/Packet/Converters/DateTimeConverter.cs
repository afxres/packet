using System;

namespace Mikodev.Network.Converters
{
    [Converter(typeof(DateTime))]
    internal sealed class DateTimeConverter : PacketConverter<DateTime>
    {
        private static byte[] ToBytes(DateTime value) => UnmanagedValueConverter<long>.ToBytes(value.ToBinary());

        private static DateTime ToValue(byte[] buffer, int offset, int length) => DateTime.FromBinary(UnmanagedValueConverter<long>.ToValue(buffer, offset, length));

        public DateTimeConverter() : base(sizeof(long)) { }

        public override byte[] GetBytes(DateTime value) => ToBytes(value);

        public override DateTime GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override byte[] GetBytes(object value) => ToBytes((DateTime)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
