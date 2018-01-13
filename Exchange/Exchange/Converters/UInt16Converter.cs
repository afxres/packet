using System;

namespace Mikodev.Network.Converters
{
    [_Converter(typeof(UInt16))]
    internal sealed class UInt16Converter : IPacketConverter, IPacketConverter<UInt16>
    {
        public int Length => sizeof(UInt16);

        public byte[] GetBytes(UInt16 value) => BitConverter.GetBytes(value);

        public UInt16 GetValue(byte[] buffer, int offset, int length) => BitConverter.ToUInt16(buffer, offset);

        byte[] IPacketConverter.GetBytes(object value) => BitConverter.GetBytes((UInt16)value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => BitConverter.ToUInt16(buffer, offset);
    }
}
