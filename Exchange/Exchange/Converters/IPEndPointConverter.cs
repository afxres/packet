using System;
using System.Net;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(IPEndPoint))]
    internal sealed class IPEndPointConverter : PacketConverter<IPEndPoint>
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

        public static IPEndPoint ToValue(byte[] buffer, int offset, int length)
        {
            var add = new IPAddress(Extension.Span(buffer, offset, length - sizeof(ushort)));
            var pot = BitConverter.ToUInt16(buffer, offset + length - sizeof(ushort));
            return new IPEndPoint(add, pot);
        }

        public override int Length => 0;

        public override byte[] GetBytes(IPEndPoint value) => ToBytes(value);

        public override IPEndPoint GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override byte[] GetBuffer(object value) => ToBytes((IPEndPoint)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
