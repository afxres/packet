using System.IO;
using ConverterDictionary = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.PacketConverter>;

namespace Mikodev.Network
{
    public sealed class PacketRawWriter
    {
        internal readonly ConverterDictionary converters;
        internal readonly MemoryStream stream = new MemoryStream(Cache.Length);

        public PacketRawWriter(ConverterDictionary converters = null) => this.converters = converters;

        public byte[] GetBytes() => stream.ToArray();

        public override string ToString() => $"{nameof(PacketRawWriter)} with {stream.Length} byte(s)";
    }
}
