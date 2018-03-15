using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(UInt64))]
    internal sealed class UInt64Converter : IPacketConverter, IPacketConverter<UInt64>
    {
        public int Length => sizeof(UInt64);

        public byte[] GetBytes(UInt64 value) => BitConverter.GetBytes(value);

        public UInt64 GetValue(byte[] buffer, int offset, int length) => BitConverter.ToUInt64(buffer, offset);

        byte[] IPacketConverter.GetBytes(object value) => BitConverter.GetBytes((UInt64)value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => BitConverter.ToUInt64(buffer, offset);
    }
}
