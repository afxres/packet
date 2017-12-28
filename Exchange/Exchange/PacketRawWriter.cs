using System;
using System.IO;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    public sealed class PacketRawWriter
    {
        internal readonly MemoryStream _mst = new MemoryStream(_Caches._StreamLength);
        internal readonly ConverterDictionary _dic;

        public PacketRawWriter(ConverterDictionary converters = null) => _dic = converters;

        internal PacketRawWriter _SetValue(byte[] buf, bool head)
        {
            if (buf != null)
                _mst._WriteOpt(buf, head);
            else if (head)
                _mst._WriteLen(0);
            return this;
        }

        [Obsolete]
        public PacketRawWriter Push(Type type, object value) => _SetValue(_Caches.GetBytes(type, _dic, value, out var hea), hea);

        [Obsolete]
        public PacketRawWriter Push<T>(T value) => _SetValue(_Caches.GetBytes(_dic, value, out var hea), hea);

        public byte[] GetBytes() => _mst.ToArray();

        public override string ToString() => $"{nameof(PacketRawWriter)} with {_mst.Length} byte(s)";
    }
}
