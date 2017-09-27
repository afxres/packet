using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    /// <summary>
    /// Raw reader without any format
    /// </summary>
    public class PacketRawReader
    {
        internal _Span _spa;
        internal readonly IReadOnlyDictionary<Type, IPacketConverter> _con;

        /// <summary>
        /// Create reader
        /// </summary>
        public PacketRawReader(PacketReader source)
        {
            _spa = new _Span(source._spa);
            _con = source._con;
        }

        /// <summary>
        /// Create reader with byte array and converters
        /// </summary>
        public PacketRawReader(byte[] buffer, IReadOnlyDictionary<Type, IPacketConverter> converters = null)
        {
            _spa = new _Span(buffer);
            _con = converters;
        }

        /// <summary>
        /// Create reader with part of byte array and converters
        /// </summary>
        public PacketRawReader(byte[] buffer, int offset, int length, IReadOnlyDictionary<Type, IPacketConverter> converters = null)
        {
            _spa = new _Span(buffer, offset, length);
            _con = converters;
        }

        /// <summary>
        /// Current position can be read
        /// </summary>
        public bool Next => _spa._idx < _spa._max;

        /// <summary>
        /// Get value with target type
        /// </summary>
        public object Pull(Type type)
        {
            var con = _Caches.Converter(type, _con, false);
            var res = default(object);
            _spa._Next(con.Length, (idx, len) => res = con.GetValue(_spa._buf, idx, len));
            return res;
        }

        /// <summary>
        /// Get value with target type (Generic)
        /// </summary>
        public T Pull<T>()
        {
            var con = _Caches.Converter(typeof(T), _con, false);
            var res = default(T);
            _spa._Next(con.Length, (idx, len) => res = con._GetValue<T>(_spa._buf, idx, len));
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
