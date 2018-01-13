using System;

namespace Mikodev.Network.Converters
{
    [_Converter(typeof(UInt32))]
    internal sealed class UInt32Converter : IPacketConverter, IPacketConverter<UInt32>
    {
        public int Length => sizeof(UInt32);

        public byte[] GetBytes(UInt32 value) => BitConverter.GetBytes(value);

        public UInt32 GetValue(byte[] buffer, int offset, int length) => BitConverter.ToUInt32(buffer, offset);

        byte[] IPacketConverter.GetBytes(object value) => BitConverter.GetBytes((UInt32)value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => BitConverter.ToUInt32(buffer, offset);
    }
}
