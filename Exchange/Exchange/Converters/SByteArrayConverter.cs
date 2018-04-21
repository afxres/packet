using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(sbyte[]))]
    internal sealed class SByteArrayConverter : IPacketConverter, IPacketConverter<sbyte[]>
    {
        public static byte[] ToBytes(sbyte[] buffer)
        {
            var buf = new byte[buffer?.Length ?? 0];
            if (buf.Length > 0)
                Buffer.BlockCopy(buffer, 0, buf, 0, buf.Length);
            return buf;
        }

        public static sbyte[] ToSbyteArray(byte[] buffer, int offset, int length)
        {
            if (length < 0 || length > (buffer?.Length ?? 0))
                throw PacketException.Overflow();
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
