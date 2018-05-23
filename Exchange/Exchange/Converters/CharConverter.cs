using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(Char))]
    internal sealed class CharConverter : PacketConverter<Char>
    {
        public override int Length => sizeof(Char);

        public override byte[] GetBytes(Char value) => BitConverter.GetBytes(value);

        public override Char GetValue(byte[] buffer, int offset, int length) => BitConverter.ToChar(buffer, offset);

        public override byte[] GetBuffer(object value) => BitConverter.GetBytes((Char)value);

        public override object GetObject(byte[] buffer, int offset, int length) => BitConverter.ToChar(buffer, offset);
    }
}
