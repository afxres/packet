using System;
using TypeTools = System.Collections.Generic.IReadOnlyDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    /// <summary>
    /// Raw reader without any format
    /// </summary>
    public sealed class PacketRawReader
    {
        internal _Element _spa;
        internal readonly TypeTools _con;

        /// <summary>
        /// Create reader
        /// </summary>
        public PacketRawReader(PacketReader source)
        {
            _spa = new _Element(source._spa);
            _con = source._con;
        }

        /// <summary>
        /// Create reader with byte array and converters
        /// </summary>
        public PacketRawReader(byte[] buffer, TypeTools converters = null)
        {
            _spa = new _Element(buffer);
            _con = converters;
        }

        /// <summary>
        /// Create reader with part of byte array and converters
        /// </summary>
        public PacketRawReader(byte[] buffer, int offset, int length, TypeTools converters = null)
        {
            _spa = new _Element(buffer, offset, length);
            _con = converters;
        }

        /// <summary>
        /// Current index before maximum
        /// </summary>
        public bool Next => _spa._idx < _spa._max;

        /// <summary>
        /// Get value with target type
        /// </summary>
        public object Pull(Type type)
        {
            var con = _Caches.Converter(type, _con, false);
            var res = _spa._Next(con);
            return res;
        }

        /// <summary>
        /// Get value with target type (Generic)
        /// </summary>
        public T Pull<T>()
        {
            var con = _Caches.Converter(typeof(T), _con, false);
            var res = _spa._Next<T>(con);
            return res;
        }

        /// <summary>
        /// Move current position to origin
        /// </summary>
        public void Reset()
        {
            _spa._idx = _spa._off;
        }

        /// <summary>
        /// Show byte count
        /// </summary>
        public override string ToString() => $"{nameof(PacketRawReader)} with {_spa._len} byte(s)";
    }
}
