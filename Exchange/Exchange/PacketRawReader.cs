using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    public sealed class PacketRawReader
    {
        internal readonly ConverterDictionary _cvt;
        internal _Element _spa;

        public PacketRawReader(PacketReader source)
        {
            _spa = new _Element(source._spa);
            _cvt = source._cvt;
        }

        public PacketRawReader(byte[] buffer, ConverterDictionary converters = null)
        {
            _spa = new _Element(buffer);
            _cvt = converters;
        }

        public PacketRawReader(byte[] buffer, int offset, int length, ConverterDictionary converters = null)
        {
            _spa = new _Element(buffer, offset, length);
            _cvt = converters;
        }

        public bool Any => _spa.Any();

        public void Reset() => _spa.Reset();

        public override string ToString() => $"{nameof(PacketRawReader)} with {_spa._len} byte(s)";
    }
}
