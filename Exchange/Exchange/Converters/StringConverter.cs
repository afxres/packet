using System.Text;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(string))]
    internal sealed class StringConverter : PacketConverter<string>
    {
        public override int Length => 0;

        public override byte[] GetBytes(string value) => Extension.s_encoding.GetBytes(value);

        public override string GetValue(byte[] buffer, int offset, int length) => Extension.s_encoding.GetString(buffer, offset, length);

        public override byte[] GetBytes(object value) => Extension.s_encoding.GetBytes((string)value);

        public override object GetObject(byte[] buffer, int offset, int length) => Extension.s_encoding.GetString(buffer, offset, length);
    }
}
