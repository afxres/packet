namespace Mikodev.Network.Converters
{
    [Converter(typeof(byte))]
    internal sealed class ByteConverter : PacketConverter<byte>
    {
        public ByteConverter() : base(sizeof(byte)) { }

        public override byte[] GetBytes(byte value) => new byte[sizeof(byte)] { value };

        public override byte GetValue(byte[] buffer, int offset, int length) => buffer[offset];

        public override byte[] GetBytes(object value) => new byte[sizeof(byte)] { (byte)value };

        public override object GetObject(byte[] buffer, int offset, int length) => buffer[offset];
    }
}
