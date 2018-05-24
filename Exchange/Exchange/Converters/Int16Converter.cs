using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(Int16))]
    internal sealed class Int16Converter : PacketConverter<Int16>
    {
        public override int Length => sizeof(Int16);

        public override byte[] GetBytes(Int16 value) => BitConverter.GetBytes(value);

        public override Int16 GetValue(byte[] buffer, int offset, int length) => BitConverter.ToInt16(buffer, offset);

        public override byte[] GetBytes(object value) => BitConverter.GetBytes((Int16)value);

        public override object GetObject(byte[] buffer, int offset, int length) => BitConverter.ToInt16(buffer, offset);
    }
}
