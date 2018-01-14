using System.IO;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    public sealed class PacketRawWriter
    {
        internal const int _Length = 256;

        internal readonly ConverterDictionary _cvt;
        internal readonly MemoryStream _str = new MemoryStream(_Length);

        public PacketRawWriter(ConverterDictionary converters = null) => _cvt = converters;

        public byte[] GetBytes() => _str.ToArray();

        public override string ToString() => $"{nameof(PacketRawWriter)} with {_str.Length} byte(s)";
    }
}
