using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(Guid))]
    internal sealed class GuidConverter : IPacketConverter, IPacketConverter<Guid>
    {
        private const int _Length = 16;

        public static byte[] ToBytes(Guid value) => value.ToByteArray();

        public static Guid ToGuid(byte[] buffer, int offset) => new Guid(_Extension.Span(buffer, offset, _Length));

        public int Length => _Length;

        public byte[] GetBytes(Guid value) => ToBytes(value);

        public Guid GetValue(byte[] buffer, int offset, int length) => ToGuid(buffer, offset);

        byte[] IPacketConverter.GetBytes(object value) => ToBytes((Guid)value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => ToGuid(buffer, offset);
    }
}
