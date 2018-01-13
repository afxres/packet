using System.Text;

namespace Mikodev.Network.Converters
{
    [_Converter(typeof(string))]
    internal sealed class StringConverter : IPacketConverter, IPacketConverter<string>
    {
        public int Length => 0;

        public byte[] GetBytes(string value) => Encoding.UTF8.GetBytes(value);

        public string GetValue(byte[] buffer, int offset, int length) => Encoding.UTF8.GetString(buffer, offset, length);

        byte[] IPacketConverter.GetBytes(object value) => Encoding.UTF8.GetBytes((string)value);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => Encoding.UTF8.GetString(buffer, offset, length);
    }
}
