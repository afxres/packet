using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class Info
    {
        internal InfoFlags From { get; set; }

        internal InfoFlags To { get; set; }

        internal Type Type { get; set; }

        internal Type IndexType { get; set; }

        internal Type ElementType { get; set; }

        internal Func<PacketReader, PacketConverter, object> ToCollection { get; set; }

        internal Func<object[], object> ToCollectionExtend { get; set; }

        internal Func<PacketReader, PacketConverter, object> ToEnumerable { get; set; }

        internal Func<PacketReader, Info, int, object> ToEnumerableAdapter { get; set; }

        internal Func<PacketReader, PacketConverter, PacketConverter, object> ToDictionary { get; set; }

        internal Func<List<object>, object> ToDictionaryExtend { get; set; }

        internal Func<PacketConverter, object, byte[][]> FromEnumerable { get; set; }

        internal Func<PacketConverter, PacketConverter, object, List<KeyValuePair<byte[], byte[]>>> FromDictionary { get; set; }

        internal Func<PacketConverter, object, IEnumerable<KeyValuePair<byte[], object>>> FromDictionaryAdapter { get; set; }

        /// <summary>
        /// Converter for enum or enum array
        /// </summary>
        internal PacketConverter Converter { get; set; }

        public override string ToString() => $"{nameof(Info)}(Type: {Type}, From: {From}, To: {To})";
    }
}
