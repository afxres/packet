using System;

namespace Mikodev.Network.Converters
{
    [Converter(typeof(bool))]
    internal sealed class BooleanConverter : PacketConverter<bool>
    {
        public override int Length => sizeof(bool);

        public override byte[] GetBytes(bool value) => BitConverter.GetBytes(value);

        public override bool GetValue(byte[] buffer, int offset, int length) => BitConverter.ToBoolean(buffer, offset);

        public override byte[] GetBytes(object value) => BitConverter.GetBytes((bool)value);

        public override object GetObject(byte[] buffer, int offset, int length) => BitConverter.ToBoolean(buffer, offset);
    }
}
