using System;
using Model = System.Char;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(Model[]))]
    internal sealed class CharArrayConverter : IPacketConverter, IPacketConverter<Model[]>
    {
        private static readonly Model[] s_empty_array = new Model[0];

        public static byte[] ToBytes(Model[] value)
        {
            if (value == null || value.Length == 0)
                return Extension.s_empty_bytes;
            var len = value.Length * sizeof(Model);
            var dst = new byte[len];
            Buffer.BlockCopy(value, 0, dst, 0, len);
            return dst;
        }

        public static Model[] ToValue(byte[] buffer, int offset, int length)
        {
            if (length == 0)
                return s_empty_array;
            if (buffer == null || (uint)length > (uint)buffer.Length || (length % sizeof(Model)) != 0)
                throw PacketException.Overflow();
            var dst = new Model[length / sizeof(Model)];
            Buffer.BlockCopy(buffer, offset, dst, 0, length);
            return dst;
        }

        public int Length => 0;

        public byte[] GetBytes(Model[] value) => ToBytes(value);

        public Model[] GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        byte[] IPacketConverter.GetBytes(object value) => ToBytes((Model[])value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
