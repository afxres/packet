using System;

namespace Mikodev.Network.Converters
{
    [_Converter(typeof(Single))]
    internal class SingleConverter : IPacketConverter, IPacketConverter<Single>
    {
        public int Length => sizeof(Single);

        public byte[] GetBytes(Single value) => BitConverter.GetBytes(value);

        public Single GetValue(byte[] buffer, int offset, int length) => BitConverter.ToSingle(buffer, offset);

        byte[] IPacketConverter.GetBytes(object value) => BitConverter.GetBytes((Single)value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => BitConverter.ToSingle(buffer, offset);
    }
}
