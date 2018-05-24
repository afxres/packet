using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(Int32))]
    internal sealed class Int32Converter : PacketConverter<Int32>
    {
        public override int Length => sizeof(Int32);

        public override byte[] GetBytes(Int32 value) => BitConverter.GetBytes(value);

        public override Int32 GetValue(byte[] buffer, int offset, int length) => BitConverter.ToInt32(buffer, offset);

        public override byte[] GetBytes(object value) => BitConverter.GetBytes((Int32)value);

        public override object GetObject(byte[] buffer, int offset, int length) => BitConverter.ToInt32(buffer, offset);
    }
}
