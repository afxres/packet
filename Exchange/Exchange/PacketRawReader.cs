using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    public sealed class PacketRawReader
    {
        internal readonly ConverterDictionary _converters;
        internal _Element _element;

        public PacketRawReader(PacketReader source)
        {
            _element = new _Element(source._element);
            _converters = source._converters;
        }

        public PacketRawReader(byte[] buffer, ConverterDictionary converters = null)
        {
            _element = new _Element(buffer);
            _converters = converters;
        }

        public PacketRawReader(byte[] buffer, int offset, int length, ConverterDictionary converters = null)
        {
            _element = new _Element(buffer, offset, length);
            _converters = converters;
        }

        public bool Any => _element.Any();

        public void Reset() => _element.Reset();

        public override string ToString() => $"{nameof(PacketRawReader)} with {_element._length} byte(s)";
    }
}
