using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(UInt16))]
    internal sealed class UInt16Converter : PacketConverter<UInt16>
    {
        public override int Length => sizeof(UInt16);

        public override byte[] GetBytes(UInt16 value) => BitConverter.GetBytes(value);

        public override UInt16 GetValue(byte[] buffer, int offset, int length) => BitConverter.ToUInt16(buffer, offset);

        public override byte[] GetBuffer(object value) => BitConverter.GetBytes((UInt16)value);

        public override object GetObject(byte[] buffer, int offset, int length) => BitConverter.ToUInt16(buffer, offset);
    }
}
