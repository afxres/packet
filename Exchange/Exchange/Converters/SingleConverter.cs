using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(Single))]
    internal sealed class SingleConverter : PacketConverter<Single>
    {
        public override int Length => sizeof(Single);

        public override byte[] GetBytes(Single value) => BitConverter.GetBytes(value);

        public override Single GetValue(byte[] buffer, int offset, int length) => BitConverter.ToSingle(buffer, offset);

        public override byte[] GetBuffer(object value) => BitConverter.GetBytes((Single)value);

        public override object GetObject(byte[] buffer, int offset, int length) => BitConverter.ToSingle(buffer, offset);
    }
}
