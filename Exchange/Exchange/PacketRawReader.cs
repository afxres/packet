using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    /// <summary>
    /// Raw reader without any format
    /// </summary>
    public sealed class PacketRawReader
    {
        internal _Portion _spa;
        internal readonly IReadOnlyDictionary<Type, IPacketConverter> _con;

        /// <summary>
        /// Create reader
        /// </summary>
        public PacketRawReader(PacketReader source)
        {
            _spa = new _Portion(source._spa);
            _con = source._con;
        }

        /// <summary>
        /// Create reader with byte array and converters
        /// </summary>
        public PacketRawReader(byte[] buffer, IReadOnlyDictionary<Type, IPacketConverter> converters = null)
        {
            _spa = new _Portion(buffer);
            _con = converters;
        }

        /// <summary>
        /// Create reader with part of byte array and converters
        /// </summary>
        public PacketRawReader(byte[] buffer, int offset, int length, IReadOnlyDictionary<Type, IPacketConverter> converters = null)
        {
            _spa = new _Portion(buffer, offset, length);
            _con = converters;
        }

        /// <summary>
        /// Current position end of block
        /// </summary>
        public bool Ended => _spa._Over();

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
    }
}
