using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(Guid))]
    internal sealed class GuidConverter : IPacketConverter, IPacketConverter<Guid>
    {
        private const int SizeOf = 16;

        public static byte[] ToBytes(Guid value) => value.ToByteArray();

        public static Guid ToGuid(byte[] buffer, int offset) => new Guid(Extension.Span(buffer, offset, SizeOf));

        public int Length => SizeOf;

        public byte[] GetBytes(Guid value) => ToBytes(value);

        public Guid GetValue(byte[] buffer, int offset, int length) => ToGuid(buffer, offset);

        byte[] IPacketConverter.GetBytes(object value) => ToBytes((Guid)value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => ToGuid(buffer, offset);
    }
}
