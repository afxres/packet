using System.Net;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(IPAddress))]
    internal sealed class IPAddressConverter : IPacketConverter, IPacketConverter<IPAddress>
    {
        public static byte[] ToBytes(IPAddress value) => value.GetAddressBytes();

        public static IPAddress ToIPAddress(byte[] buffer, int offset, int length) => new IPAddress(_Extension.Span(buffer, offset, length));

        public int Length => 0;

        public byte[] GetBytes(IPAddress value) => ToBytes(value);

        public IPAddress GetValue(byte[] buffer, int offset, int length) => ToIPAddress(buffer, offset, length);

        byte[] IPacketConverter.GetBytes(object value) => ToBytes((IPAddress)value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => ToIPAddress(buffer, offset, length);
    }
}
