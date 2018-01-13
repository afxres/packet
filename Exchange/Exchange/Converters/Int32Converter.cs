using System;

namespace Mikodev.Network.Converters
{
    [_Converter(typeof(Int32))]
    internal sealed class Int32Converter : IPacketConverter, IPacketConverter<Int32>
    {
        public int Length => sizeof(Int32);

        public byte[] GetBytes(Int32 value) => BitConverter.GetBytes(value);

        public Int32 GetValue(byte[] buffer, int offset, int length) => BitConverter.ToInt32(buffer, offset);

        byte[] IPacketConverter.GetBytes(object value) => BitConverter.GetBytes((Int32)value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => BitConverter.ToInt32(buffer, offset);
    }
}
