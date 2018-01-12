using System;

namespace Mikodev.Network.Converters
{
    [_Converter(typeof(Boolean))]
    internal class BooleanConverter : IPacketConverter, IPacketConverter<Boolean>
    {
        public int Length => sizeof(Boolean);

        public byte[] GetBytes(Boolean value) => BitConverter.GetBytes(value);

        public Boolean GetValue(byte[] buffer, int offset, int length) => BitConverter.ToBoolean(buffer, offset);

        byte[] IPacketConverter.GetBytes(object value) => BitConverter.GetBytes((Boolean)value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => BitConverter.ToBoolean(buffer, offset);
    }
}
