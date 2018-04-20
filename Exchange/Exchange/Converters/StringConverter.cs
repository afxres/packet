using System.Text;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(string))]
    internal sealed class StringConverter : IPacketConverter, IPacketConverter<string>
    {
        public int Length => 0;

        public byte[] GetBytes(string value) => Extension.s_encoding.GetBytes(value);

        public string GetValue(byte[] buffer, int offset, int length) => Extension.s_encoding.GetString(buffer, offset, length);

        byte[] IPacketConverter.GetBytes(object value) => Extension.s_encoding.GetBytes((string)value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => Extension.s_encoding.GetString(buffer, offset, length);
    }
}
