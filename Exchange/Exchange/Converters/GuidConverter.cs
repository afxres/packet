using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(Guid))]
    internal sealed class GuidConverter : PacketConverter<Guid>
    {
        private const int SizeOf = 16;

        public static byte[] ToBytes(Guid value) => value.ToByteArray();

        public static Guid ToValue(byte[] buffer, int offset) => new Guid(Extension.Span(buffer, offset, SizeOf));

        public override int Length => SizeOf;

        public override byte[] GetBytes(Guid value) => ToBytes(value);

        public override Guid GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset);

        public override byte[] GetBytes(object value) => ToBytes((Guid)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset);
    }
}
