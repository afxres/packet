using Mikodev.Network.Internal;
using System.Net;

namespace Mikodev.Network.Converters
{
    [Converter(typeof(IPAddress))]
    internal sealed class IPAddressConverter : PacketConverter<IPAddress>
    {
        private static byte[] ToBytes(IPAddress value) => value != null ? value.GetAddressBytes() : Empty.Array<byte>();

        private static IPAddress ToValue(byte[] buffer, int offset, int length)
        {
            if (length == 0)
                return null;
            if (buffer == null || offset < 0 || length < 1 || buffer.Length - offset < length)
                throw PacketException.Overflow();
            var result = new byte[length];
            Unsafe.Copy(ref result[0], in buffer[offset], length);
            return new IPAddress(result);
        }

        public IPAddressConverter() : base(0) { }

        public override byte[] GetBytes(IPAddress value) => ToBytes(value);

        public override IPAddress GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override byte[] GetBytes(object value) => ToBytes((IPAddress)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
