using System;
using System.Collections.Generic;
using System.IO;

namespace Mikodev.Network
{
    /// <summary>
    /// Raw writer without any format
    /// </summary>
    public class PacketRawWriter
    {
        internal readonly List<byte[]> _lst = new List<byte[]>();
        internal readonly IReadOnlyDictionary<Type, IPacketConverter> _con;

        /// <summary>
        /// Create writer with converters
        /// </summary>
        public PacketRawWriter(IReadOnlyDictionary<Type, IPacketConverter> converters = null) => _con = converters;

        /// <summary>
        /// Writer value with target type
        /// </summary>
        public PacketRawWriter Push(Type type, object value)
        {
            var con = _Caches.Converter(type, _con, false);
            var buf = con.GetBytes(value);
            if (con.Length == null)
                _lst.Add(BitConverter.GetBytes(buf?.Length ?? 0));
            _lst.Add(buf);
            return this;
        }

        /// <summary>
        /// Writer value with target type (Generic)
        /// </summary>
        public PacketRawWriter Push<T>(T value)
        {
            var con = _Caches.Converter(typeof(T), _con, false);
            var buf = con._GetBytes(value);
            if (con.Length == null)
                _lst.Add(BitConverter.GetBytes(buf?.Length ?? 0));
            _lst.Add(buf);
            return this;
        }

        internal void _Write(Stream stream)
        {
            foreach (var i in _lst)
            {
                if (i == null)
                    continue;
                stream.Write(i, 0, i.Length);
            }
        }

        /// <summary>
        /// Get binary packet
        /// </summary>
        public byte[] GetBytes()
        {
            var mst = new MemoryStream();
            _Write(mst);
            return mst.ToArray();
        }
    }
}
