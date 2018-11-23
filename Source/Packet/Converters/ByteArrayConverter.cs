namespace Mikodev.Network.Converters
{
    [Converter(typeof(byte[]))]
    internal sealed class ByteArrayConverter : PacketConverter<byte[]>
    {
        public ByteArrayConverter() : base(0) { }

        public override byte[] GetBytes(byte[] value) => value ?? Empty.Array<byte>();

        public override byte[] GetBytes(object value) => value != null ? (byte[])value : Empty.Array<byte>();

        public override byte[] GetValue(byte[] buffer, int offset, int length) => UnmanagedArrayConverter<byte>.ToValue(buffer, offset, length);

        public override object GetObject(byte[] buffer, int offset, int length) => UnmanagedArrayConverter<byte>.ToValue(buffer, offset, length);
    }
}
