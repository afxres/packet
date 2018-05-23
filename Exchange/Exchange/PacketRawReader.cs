using ConverterDictionary = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.PacketConverter>;

namespace Mikodev.Network
{
    public sealed class PacketRawReader
    {
        internal readonly ConverterDictionary converters;
        private readonly Element element;
        private int index;

        public PacketRawReader(PacketReader source)
        {
            converters = source.converters;
            element = source.element;
            index = source.element.offset;
        }

        public PacketRawReader(byte[] buffer, ConverterDictionary converters = null)
        {
            this.converters = converters;
            element = new Element(buffer);
            index = 0;
        }

        public PacketRawReader(byte[] buffer, int offset, int length, ConverterDictionary converters = null)
        {
            element = new Element(buffer, offset, length);
            this.converters = converters;
        }

        internal object Next(PacketConverter converter) => element.Next(ref index, converter);

        internal T NextAuto<T>(PacketConverter converter) => element.NextAuto<T>(ref index, converter);

        public bool Any => index < element.Max;

        public void Reset() => index = element.offset;

        public override string ToString() => $"{nameof(PacketRawReader)} with {element.length} byte(s)";
    }
}
