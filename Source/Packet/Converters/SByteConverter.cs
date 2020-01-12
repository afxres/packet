using Mikodev.Network.Internal;

namespace Mikodev.Network.Converters
{
    [Converter(typeof(sbyte))]
    internal sealed class SByteConverter : PacketConverter<sbyte>
    {
        public SByteConverter() : base(sizeof(sbyte)) { }

        public override byte[] GetBytes(sbyte value) => new byte[sizeof(sbyte)] { (byte)value };

        public override sbyte GetValue(byte[] buffer, int offset, int length) => (sbyte)buffer[offset];

        public override byte[] GetBytes(object value) => new byte[sizeof(sbyte)] { (byte)(sbyte)value };

        public override object GetObject(byte[] buffer, int offset, int length) => (sbyte)buffer[offset];
    }
}
