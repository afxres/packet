using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(byte[]))]
    internal sealed class ByteArrayConverter : IPacketConverter, IPacketConverter<byte[]>
    {
        public static byte[] ToBytes(byte[] buffer) => buffer ?? Extension.s_empty_bytes;

        public static byte[] ToByteArray(byte[] buffer, int offset, int length)
        {
            var len = buffer?.Length ?? 0;
            if (length < 0 || length > len)
                throw PacketException.Overflow();
            var buf = new byte[length];
            if (length > 0)
                Buffer.BlockCopy(buffer, offset, buf, 0, length);
            return buf;
        }

        public int Length => 0;

        public byte[] GetBytes(byte[] value) => ToBytes(value);

        public byte[] GetValue(byte[] buffer, int offset, int length) => ToByteArray(buffer, offset, length);

        byte[] IPacketConverter.GetBytes(object value) => ToBytes((byte[])value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => ToByteArray(buffer, offset, length);
    }
}
