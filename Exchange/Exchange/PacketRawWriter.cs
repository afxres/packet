using ConverterDictionary = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.PacketConverter>;

namespace Mikodev.Network
{
    public sealed class PacketRawWriter
    {
        internal readonly ConverterDictionary converters;
        internal readonly UnsafeStream stream = new UnsafeStream();

        public PacketRawWriter(ConverterDictionary converters = null) => this.converters = converters;

        public byte[] GetBytes() => stream.GetBytes();

        public override string ToString() => $"{nameof(PacketRawWriter)} with {stream.GetPosition()} byte(s)";
    }
}
