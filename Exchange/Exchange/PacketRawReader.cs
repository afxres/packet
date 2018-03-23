using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    public sealed class PacketRawReader
    {
        internal readonly ConverterDictionary _cvt;
        private readonly _Element _ele;
        private int _idx;

        public PacketRawReader(PacketReader source)
        {
            _cvt = source._cvt;
            _ele = source._ele;
            _idx = source._ele._off;
        }

        public PacketRawReader(byte[] buffer, ConverterDictionary converters = null)
        {
            _cvt = converters;
            _ele = new _Element(buffer);
            _idx = 0;
        }

        public PacketRawReader(byte[] buffer, int offset, int length, ConverterDictionary converters = null)
        {
            _ele = new _Element(buffer, offset, length);
            _cvt = converters;
        }

        internal object Next(IPacketConverter con) => _ele.Next(ref _idx, con);

        internal T NextAuto<T>(IPacketConverter con) => _ele.NextAuto<T>(ref _idx, con);

        public bool Any => _idx < _ele.Max();

        public void Reset() => _idx = _ele._off;

        public override string ToString() => $"{nameof(PacketRawReader)} with {_ele._len} byte(s)";
    }
}
