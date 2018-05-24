namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(byte))]
    internal sealed class ByteConverter : PacketConverter<byte>
    {
        public override int Length => sizeof(byte);

        public override byte[] GetBytes(byte value) => new byte[] { value };

        public override byte GetValue(byte[] buffer, int offset, int length) => buffer[offset];

        public override byte[] GetBytes(object value) => new byte[] { (byte)value };

        public override object GetObject(byte[] buffer, int offset, int length) => buffer[offset];
    }
}
