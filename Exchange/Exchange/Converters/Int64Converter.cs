using System;

namespace Mikodev.Network.Converters
{
    [_Converter(typeof(Int64))]
    internal sealed class Int64Converter : IPacketConverter, IPacketConverter<Int64>
    {
        public int Length => sizeof(Int64);

        public byte[] GetBytes(Int64 value) => BitConverter.GetBytes(value);

        public Int64 GetValue(byte[] buffer, int offset, int length) => BitConverter.ToInt64(buffer, offset);

        byte[] IPacketConverter.GetBytes(object value) => BitConverter.GetBytes((Int64)value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => BitConverter.ToInt64(buffer, offset);
    }
}
