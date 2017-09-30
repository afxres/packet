using System;
using System.IO;
using TypeTools = System.Collections.Generic.IReadOnlyDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    /// <summary>
    /// Raw writer without any format
    /// </summary>
    public sealed class PacketRawWriter
    {
        internal readonly MemoryStream _mst = new MemoryStream(_Caches._StrInit);
        internal readonly TypeTools _dic;

        /// <summary>
        /// Create writer with converters
        /// </summary>
        public PacketRawWriter(TypeTools converters = null) => _dic = converters;

        internal PacketRawWriter _Push(byte[] buf, bool head)
        {
            if (buf != null)
                _mst._WriteExt(buf, head);
            else if (head)
                _mst._WriteLen(0);
            return this;
        }

        /// <summary>
        /// Writer value with target type
        /// </summary>
        public PacketRawWriter Push(Type type, object value) => _Push(_Caches.GetBytes(type, _dic, value, out var hea), hea);

        /// <summary>
        /// Writer value with target type (Generic)
        /// </summary>
        public PacketRawWriter Push<T>(T value) => _Push(_Caches.GetBytes(_dic, value, out var hea), hea);

        /// <summary>
        /// Get binary packet
        /// </summary>
        public byte[] GetBytes() => _mst.ToArray();

        /// <summary>
        /// Show byte count
        /// </summary>
        public override string ToString() => $"{nameof(PacketRawWriter)} with {_mst.Length} byte(s)";
    }
}
