using Mikodev.Network.Internal;
using System.Net;

namespace Mikodev.Network.Converters
{
    [Converter(typeof(IPEndPoint))]
    internal sealed class IPEndPointConverter : PacketConverter<IPEndPoint>
    {
        private static byte[] ToBytes(IPEndPoint value)
        {
            if (value == null)
                return Empty.Array<byte>();
            var address = value.Address.GetAddressBytes();
            var result = new byte[address.Length + sizeof(ushort)];
            Unsafe.Copy(ref result[0], in address[0], address.Length);
            UnmanagedValueConverter<ushort>.UnsafeToBytes(ref result[address.Length], (ushort)value.Port);
            return result;
        }

        private static IPEndPoint ToValue(byte[] buffer, int offset, int length)
        {
            if (length == 0)
                return null;
            var addressLength = length - sizeof(ushort);
            if (buffer == null || offset < 0 || addressLength < 1 || buffer.Length - offset < length)
                throw PacketException.Overflow();
            var addressBuffer = new byte[addressLength];
            Unsafe.Copy(ref addressBuffer[0], in buffer[offset], addressLength);
            var address = new IPAddress(addressBuffer);
            var port = UnmanagedValueConverter<ushort>.UnsafeToValue(ref buffer[offset + addressLength]);
            return new IPEndPoint(address, port);
        }

        public IPEndPointConverter() : base(0) { }

        public override byte[] GetBytes(IPEndPoint value) => ToBytes(value);

        public override IPEndPoint GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override byte[] GetBytes(object value) => ToBytes((IPEndPoint)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
