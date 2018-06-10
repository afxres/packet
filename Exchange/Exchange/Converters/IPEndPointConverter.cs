using System.Net;
using System.Runtime.CompilerServices;

namespace Mikodev.Network.Converters
{
    [Converter(typeof(IPEndPoint))]
    internal sealed class IPEndPointConverter : PacketConverter<IPEndPoint>
    {
        private static byte[] ToBytes(IPEndPoint value)
        {
            var address = value.Address.GetAddressBytes();
            var result = new byte[address.Length + sizeof(ushort)];
            Unsafe.CopyBlockUnaligned(ref result[0], ref address[0], (uint)address.Length);
            Unsafe.WriteUnaligned(ref result[address.Length], (ushort)value.Port);
            return result;
        }

        private static IPEndPoint ToValue(byte[] buffer, int offset, int length)
        {
            var addressLength = length - sizeof(ushort);
            if (buffer == null || offset < 0 || addressLength < 0 || buffer.Length - offset < length)
                throw PacketException.Overflow();
            var addressBuffer = new byte[addressLength];
            Unsafe.CopyBlockUnaligned(ref addressBuffer[0], ref buffer[offset], (uint)addressLength);
            var address = new IPAddress(addressBuffer);
            var port = Unsafe.ReadUnaligned<ushort>(ref buffer[offset + addressLength]);
            return new IPEndPoint(address, port);
        }

        public IPEndPointConverter() : base(0) { }

        public override byte[] GetBytes(IPEndPoint value) => ToBytes(value);

        public override IPEndPoint GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override byte[] GetBytes(object value) => ToBytes((IPEndPoint)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
