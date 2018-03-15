using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(sbyte[]))]
    internal sealed class SByteArrayConverter : IPacketConverter, IPacketConverter<sbyte[]>
    {
        public static byte[] ToBytes(sbyte[] buffer)
        {
            var len = buffer?.Length ?? 0;
            var buf = new byte[len];
            if (len > 0)
                Buffer.BlockCopy(buffer, 0, buf, 0, len);
            return buf;
        }

        public static sbyte[] ToSbyteArray(byte[] buffer, int offset, int length)
        {
            var len = buffer?.Length ?? 0;
            if (length < 0 || length > len)
                throw new PacketException(PacketError.Overflow);
            var buf = new sbyte[length];
            if (length > 0)
                Buffer.BlockCopy(buffer, offset, buf, 0, length);
            return buf;
        }

        public int Length => 0;

        public byte[] GetBytes(sbyte[] value) => ToBytes(value);

        public sbyte[] GetValue(byte[] buffer, int offset, int length) => ToSbyteArray(buffer, offset, length);

        byte[] IPacketConverter.GetBytes(object value) => ToBytes((sbyte[])value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => ToSbyteArray(buffer, offset, length);
    }
}
