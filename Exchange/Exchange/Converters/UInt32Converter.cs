using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(UInt32))]
    internal sealed class UInt32Converter : PacketConverter<UInt32>
    {
        public override int Length => sizeof(UInt32);

        public override byte[] GetBytes(UInt32 value) => BitConverter.GetBytes(value);

        public override UInt32 GetValue(byte[] buffer, int offset, int length) => BitConverter.ToUInt32(buffer, offset);

        public override byte[] GetBytes(object value) => BitConverter.GetBytes((UInt32)value);

        public override object GetObject(byte[] buffer, int offset, int length) => BitConverter.ToUInt32(buffer, offset);
    }
}
