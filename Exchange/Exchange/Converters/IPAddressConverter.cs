using System.Net;

namespace Mikodev.Network.Converters
{
    [Converter(typeof(IPAddress))]
    internal sealed class IPAddressConverter : PacketConverter<IPAddress>
    {
        private static byte[] ToBytes(IPAddress value) => value.GetAddressBytes();

        private static IPAddress ToValue(byte[] buffer, int offset, int length) => new IPAddress(Extension.BorrowOrCopy(buffer, offset, length));

        public IPAddressConverter() : base(0) { }

        public override byte[] GetBytes(IPAddress value) => ToBytes(value);

        public override IPAddress GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override byte[] GetBytes(object value) => ToBytes((IPAddress)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
