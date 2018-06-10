namespace Mikodev.Network.Converters
{
    [Converter(typeof(string))]
    internal sealed class StringConverter : PacketConverter<string>
    {
        public StringConverter() : base(0) { }

        public override byte[] GetBytes(string value) => Extension.Encoding.GetBytes(value);

        public override string GetValue(byte[] buffer, int offset, int length) => Extension.Encoding.GetString(buffer, offset, length);

        public override byte[] GetBytes(object value) => Extension.Encoding.GetBytes((string)value);

        public override object GetObject(byte[] buffer, int offset, int length) => Extension.Encoding.GetString(buffer, offset, length);
    }
}
