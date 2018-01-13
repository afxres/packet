using System;

namespace Mikodev.Network.Converters
{
    [_Converter(typeof(Int16))]
    internal sealed class Int16Converter : IPacketConverter, IPacketConverter<Int16>
    {
        public int Length => sizeof(Int16);

        public byte[] GetBytes(Int16 value) => BitConverter.GetBytes(value);

        public Int16 GetValue(byte[] buffer, int offset, int length) => BitConverter.ToInt16(buffer, offset);

        byte[] IPacketConverter.GetBytes(object value) => BitConverter.GetBytes((Int16)value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => BitConverter.ToInt16(buffer, offset);
    }
}
