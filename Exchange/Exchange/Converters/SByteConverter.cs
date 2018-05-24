namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(sbyte))]
    internal sealed class SByteConverter : PacketConverter<sbyte>
    {
        public override int Length => sizeof(sbyte);

        public override byte[] GetBytes(sbyte value) => new byte[] { (byte)value };

        public override sbyte GetValue(byte[] buffer, int offset, int length) => (sbyte)buffer[offset];

        public override byte[] GetBytes(object value) => new byte[] { (byte)(sbyte)value };

        public override object GetObject(byte[] buffer, int offset, int length) => buffer[offset];
    }
}
