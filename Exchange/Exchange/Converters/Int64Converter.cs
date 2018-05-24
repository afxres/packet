using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(Int64))]
    internal sealed class Int64Converter : PacketConverter<Int64>
    {
        public override int Length => sizeof(Int64);

        public override byte[] GetBytes(Int64 value) => BitConverter.GetBytes(value);

        public override Int64 GetValue(byte[] buffer, int offset, int length) => BitConverter.ToInt64(buffer, offset);

        public override byte[] GetBytes(object value) => BitConverter.GetBytes((Int64)value);

        public override object GetObject(byte[] buffer, int offset, int length) => BitConverter.ToInt64(buffer, offset);
    }
}
