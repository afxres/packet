namespace Mikodev.Network.Converters
{
    [_Converter(typeof(sbyte))]
    internal sealed class SByteConverter : IPacketConverter, IPacketConverter<sbyte>
    {
        public int Length => sizeof(sbyte);

        public byte[] GetBytes(sbyte value) => new byte[] { (byte)value };

        public sbyte GetValue(byte[] buffer, int offset, int length) => (sbyte)buffer[offset];

        byte[] IPacketConverter.GetBytes(object value) => new byte[] { (byte)(sbyte)value };

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => buffer[offset];
    }
}
