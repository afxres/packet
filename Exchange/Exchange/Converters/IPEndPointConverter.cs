using System;
using System.Net;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(IPEndPoint))]
    internal sealed class IPEndPointConverter : IPacketConverter, IPacketConverter<IPEndPoint>
    {
        public static byte[] ToBytes(IPEndPoint value)
        {
            var add = value.Address.GetAddressBytes();
            var pot = BitConverter.GetBytes((ushort)value.Port);
            var res = new byte[add.Length + pot.Length];
            Buffer.BlockCopy(add, 0, res, 0, add.Length);
            Buffer.BlockCopy(pot, 0, res, add.Length, pot.Length);
            return res;
        }

        public static IPEndPoint ToIPEndPoint(byte[] buffer, int offset, int length)
        {
            var add = new IPAddress(Extension.Span(buffer, offset, length - sizeof(ushort)));
            var pot = BitConverter.ToUInt16(buffer, offset + length - sizeof(ushort));
            return new IPEndPoint(add, pot);
        }

        public int Length => 0;

        public byte[] GetBytes(IPEndPoint value) => ToBytes(value);

        public IPEndPoint GetValue(byte[] buffer, int offset, int length) => ToIPEndPoint(buffer, offset, length);

        byte[] IPacketConverter.GetBytes(object value) => ToBytes((IPEndPoint)value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => ToIPEndPoint(buffer, offset, length);
    }
}
