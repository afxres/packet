using System;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    public sealed class PacketRawReader
    {
        internal _Element _spa;
        internal readonly ConverterDictionary _con;

        public PacketRawReader(PacketReader source)
        {
            _spa = new _Element(source._spa);
            _con = source._con;
        }

        public PacketRawReader(byte[] buffer, ConverterDictionary converters = null)
        {
            _spa = new _Element(buffer);
            _con = converters;
        }

        public PacketRawReader(byte[] buffer, int offset, int length, ConverterDictionary converters = null)
        {
            _spa = new _Element(buffer, offset, length);
            _con = converters;
        }

        public bool Any => _spa.Any();

        [Obsolete]
        public object Pull(Type type)
        {
            var con = _Caches.Converter(type, _con, false);
            var res = _spa.Next(con);
            return res;
        }

        [Obsolete]
        public T Pull<T>()
        {
            var con = _Caches.Converter(typeof(T), _con, false);
            var res = _spa.NextAuto<T>(con);
            return res;
        }

        public void Reset()
        {
            _spa._idx = _spa._off;
        }

        public override string ToString() => $"{nameof(PacketRawReader)} with {_spa._len} byte(s)";
    }
}
