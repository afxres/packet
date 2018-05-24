using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(UInt64))]
    internal sealed class UInt64Converter : PacketConverter<UInt64>
    {
        public override int Length => sizeof(UInt64);

        public override byte[] GetBytes(UInt64 value) => BitConverter.GetBytes(value);

        public override UInt64 GetValue(byte[] buffer, int offset, int length) => BitConverter.ToUInt64(buffer, offset);

        public override byte[] GetBytes(object value) => BitConverter.GetBytes((UInt64)value);

        public override object GetObject(byte[] buffer, int offset, int length) => BitConverter.ToUInt64(buffer, offset);
    }
}
