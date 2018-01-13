using System;

namespace Mikodev.Network.Converters
{
    [_Converter(typeof(Char))]
    internal sealed class CharConverter : IPacketConverter, IPacketConverter<Char>
    {
        public int Length => sizeof(Char);

        public byte[] GetBytes(Char value) => BitConverter.GetBytes(value);

        public Char GetValue(byte[] buffer, int offset, int length) => BitConverter.ToChar(buffer, offset);

        byte[] IPacketConverter.GetBytes(object value) => BitConverter.GetBytes((Char)value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => BitConverter.ToChar(buffer, offset);
    }
}
