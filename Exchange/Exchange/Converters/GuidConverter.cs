using System;

namespace Mikodev.Network.Converters
{
    [Converter(typeof(Guid))]
    internal sealed class GuidConverter : PacketConverter<Guid>
    {
        private const int SizeOf = 16;

        private static byte[] ToBytes(Guid value) => value.ToByteArray();

        private static Guid ToValue(byte[] buffer, int offset, int length)
        {
            if (length < SizeOf)
                throw PacketException.Overflow();
            var target = Extension.BorrowOrCopy(buffer, offset, SizeOf);
            var result = new Guid(target);
            return result;
        }

        public GuidConverter() : base(SizeOf) { }

        public override byte[] GetBytes(Guid value) => ToBytes(value);

        public override Guid GetValue(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);

        public override byte[] GetBytes(object value) => ToBytes((Guid)value);

        public override object GetObject(byte[] buffer, int offset, int length) => ToValue(buffer, offset, length);
    }
}
