using System.IO;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    public sealed class PacketRawWriter
    {
        internal const int _Length = 256;

        internal readonly ConverterDictionary _converters;
        internal readonly MemoryStream _stream = new MemoryStream(_Length);

        public PacketRawWriter(ConverterDictionary converters = null) => _converters = converters;

        public byte[] GetBytes() => _stream.ToArray();

        public override string ToString() => $"{nameof(PacketRawWriter)} with {_stream.Length} byte(s)";
    }
}
