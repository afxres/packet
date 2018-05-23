using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(Boolean))]
    internal sealed class BooleanConverter : PacketConverter<Boolean>
    {
        public override int Length => sizeof(Boolean);

        public override byte[] GetBytes(Boolean value) => BitConverter.GetBytes(value);

        public override Boolean GetValue(byte[] buffer, int offset, int length) => BitConverter.ToBoolean(buffer, offset);

        public override byte[] GetBuffer(object value) => BitConverter.GetBytes((Boolean)value);

        public override object GetObject(byte[] buffer, int offset, int length) => BitConverter.ToBoolean(buffer, offset);
    }
}
