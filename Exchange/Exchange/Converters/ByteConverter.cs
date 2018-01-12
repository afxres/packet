namespace Mikodev.Network.Converters
{
    [_Converter(typeof(byte))]
    internal class ByteConverter : IPacketConverter, IPacketConverter<byte>
    {
        public int Length => sizeof(byte);

        public byte[] GetBytes(byte value) => new byte[] { value };

        public byte GetValue(byte[] buffer, int offset, int length) => buffer[offset];

        byte[] IPacketConverter.GetBytes(object value) => new byte[] { (byte)value };

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => buffer[offset];
    }
}
