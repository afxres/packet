using System.Net;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(IPAddress))]
    internal sealed class IPAddressConverter : PacketConverter<IPAddress>
    {
        public static byte[] ToBytes(IPAddress value) => value.GetAddressBytes();

        public static IPAddress ToValue(byte[] buffer, int offset, int length) => new IPAddress(Extension.Span(buffer, offset, length));

        public override int Length => 0;

        public override byte[] GetBytes(IPAddress value) => ToBytes(value);

        public override IPAddress GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override byte[] GetBuffer(object value) => ToBytes((IPAddress)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
